namespace Angara.Serialization

open System
open System.Reflection
open System.Diagnostics
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns

[<Interface>]
type ISerializerLibrary =
    inherit ISerializerResolver
    abstract Name: string with get
    abstract Register<'a> : string * (ISerializerResolver * 'a -> InfoSet) * (ISerializerResolver * InfoSet -> 'a) -> unit // Non-generic serializer registration
    abstract RegisterGeneric: string * Expr -> unit // Generic serializer registration
    abstract RegisterTransient: string * Type -> unit
    abstract GetRegistrars: unit -> MethodBase[]

type SerializerLibrary (name: string) =

    // TypeId -> Artefact type * Serializer instance (null for transient) * Registrar method (maybe None)
    let mutable serializers = Map.empty<string, Type * ISerializer * MethodBase option>     
    // Assembly qualified CLR type -> TypeId
    let mutable typeids = Map.empty<string, string> 
    // TypeId -> Generic artefact type * MethodInfo (null for transient) * Registrar method (maybe None)
    let mutable generics = Map.empty<string, Type * MethodInfo option * MethodBase option>     
    
    let tryGetGenericArguments (g : Type) (a : Type) (genArgs : Type[]) : Type[] option =
        let actArgs = Array.zeroCreate<Type> genArgs.Length
    
        let rec tryMatchTypes (g : System.Type, a : System.Type) : bool =
            if g.IsGenericParameter 
            then         
                let pos = g.GenericParameterPosition
                pos < genArgs.Length && 
                genArgs.[pos].Equals(g) &&
                match actArgs.[pos] with
                | null -> actArgs.[pos] <- a
                          true
                | aa -> actArgs.[pos].Equals(aa)
            else
                g.Name = a.Name && g.Namespace = a.Namespace && g.Assembly.FullName = a.Assembly.FullName &&
                if g.ContainsGenericParameters then 
                    let ga = g.GenericTypeArguments
                    let aa = a.GenericTypeArguments 
                    ga.Length = aa.Length && Array.zip ga aa |> Seq.forall tryMatchTypes
                else g.Equals(a)

        if tryMatchTypes(g, a) then Some(actArgs) else None

    let getRegistrar (s : StackTrace) =
        let chooser (frame : StackFrame) = 
            let m = frame.GetMethod()
            if m.IsStatic 
            then let p = m.GetParameters()
                 if p.Length = 1 && p.[0].ParameterType = typeof<ISerializerLibrary seq> then Some(m) else None
            else None
        s.GetFrames() |> Array.tryPick chooser
        
    let registerInstance typeId atype so (reg : MethodBase option) =
        if serializers.ContainsKey(typeId) then raise(InvalidOperationException(sprintf "The TypeId %s is already registered for generic serializer" typeId))                               
        let tryGetType obj = if obj = null then null else obj.GetType()
        let stype = tryGetType so
        match serializers.TryFind(typeId) with
        | Some(atype2, so2, reg2) -> let stype2 = tryGetType so
                                     if atype <> atype2 then raise(InvalidOperationException(sprintf "Attempt to register multiple artefact types (%s) and (%s) with one TypeId (%s)" so2.Type.FullName atype.FullName typeId))                               
                                     if stype <> stype2 then raise(InvalidOperationException(sprintf "Attempt to register multiple serializers (%s) and (%s) with one TypeId (%s)" stype2.FullName stype.FullName typeId))
                                     // Update registrar method if it was not previously recorded
                                     if reg2.IsNone && reg.IsSome then serializers <- serializers.Remove(typeId).Add(typeId, (atype, so, reg))
        | None -> serializers <- serializers.Add(typeId, (atype,so, reg))
                  typeids <- typeids.Add(atype.AssemblyQualifiedName, typeId)

//    let buildSerializer (r : ISerializerResolver) (t : Type) (typeId : string) (typeArgs : Type[]) (pair : obj) = 
//        let rr = typeArgs |> Seq.map (r.TryResolveType r) |> List.ofSeq
//        match rr |> Seq.tryFind (fun r -> match r with | NotFound(_) -> true | _ -> false) with
//        | Some(nf) -> nf
//        | None -> let typeIds = rr |> List.map(fun r -> match r with
//                                                        | Serializable(so) -> so.TypeId
//                                                        | Transient(_, typeId) -> typeId) 
//        { new ISerializer with
//            member x.Type = t
//            member x.TypeId = TypeId.Generic(typeId, )
//        }

    interface ISerializerLibrary with
        member x.TryResolveType root t =
            let t = if t.BaseType <> null && t.BaseType = t.DeclaringType then t.BaseType else t // Special case for enums
            match typeids.TryFind(t.AssemblyQualifiedName) with
            | Some(typeId) -> match serializers.TryFind typeId with
                              | None -> NotFound (t.FullName)
                              | Some(t, null, _) -> Transient(t, typeId)
                              | Some(_, so, _) -> Serializable(so)
            | None when not (t.IsGenericType) -> NotFound (t.FullName)
            | _ -> match typeids.TryFind(t.GetGenericTypeDefinition().AssemblyQualifiedName) with
                   | Some(typeId) -> match generics.TryFind typeId with
                                     | None -> NotFound (t.FullName)
                                     | Some(at, Some(md), _) -> match tryGetGenericArguments at t (md.GetGenericArguments()) with
                                                                | Some(args) -> try let m = md.MakeGenericMethod(args)
                                                                                    Serializable(IntSerializer.Instance)
                                                                                with _ -> NotFound(t.FullName)                                        
                                                                | None -> NotFound(t.FullName)                                        
                                     | Some(_, None, _) -> Transient(t, typeId)
                   | None -> NotFound (t.FullName)
        member x.TryResolveTypeId root typeId =
            match serializers.TryFind typeId with
            | None -> NotFound typeId
            | Some(t, null, _) -> Transient(t, typeId)
            | Some(_, so, _) -> Serializable(so)

        member x.Name = name

        member x.Register<'a>(typeId, serializer, deserializer) = 
            let trace = StackTrace()
            let s = { new ISerializer with
                          member x.Type = typeof<'a>
                          member x.TypeId = typeId
                          member x.Serialize r a = serializer(r, a :?> 'a)
                          member x.Deserialize r i = upcast deserializer(r, i) }
            registerInstance s.TypeId typeof<'a> s (getRegistrar trace)
    
        member x.RegisterTransient(typeId : string, a : Type) = 
            let trace = StackTrace()
            registerInstance typeId a null (getRegistrar trace)

        member x.RegisterGeneric(typeId, q) =
            let wrongQ() = failwith "Expecting quotation of static generic method returning tuple of serializer and deserializer functions for 'a"
            let tupleTD = typedefof<_*_>
            let funcTD = typedefof<_ -> _>
            match q with
            | Call(_, mi, _) ->
                if not (mi.IsGenericMethod && mi.IsStatic) then wrongQ()
                let md = mi.GetGenericMethodDefinition()
                let rt = md.ReturnType 
                if not (rt.IsGenericType && rt.GetGenericTypeDefinition().Equals(tupleTD)) then wrongQ()
                let gt = rt.GenericTypeArguments;
                if not (gt.[0].GetGenericTypeDefinition().Equals(funcTD) && gt.[1].GetGenericTypeDefinition().Equals(funcTD)) then wrongQ();
                let at = gt.[1].GenericTypeArguments.[1]
                if serializers.ContainsKey(typeId) then raise(InvalidOperationException(sprintf "The TypeId %s is already registered for non-generic serializer" typeId)) 
                let reg = (getRegistrar (StackTrace()))
                match generics.TryFind typeId with
                | Some(_, None, _) -> raise(InvalidOperationException(sprintf "The TypeId %s is already registered for transient generic type" typeId))
                | Some(at2, Some(md2), reg2) -> 
                    if not(at2.Equals(at)) then raise(InvalidOperationException(sprintf "The TypeId %s is already registered for generic serializer of another type" typeId))
                    if md2 <> md then raise(InvalidOperationException(sprintf "The TypeId %s is already registered for generic serializer" typeId))
                    if reg2.IsNone && reg.IsSome then generics <- generics.Remove(typeId).Add(typeId, (at, Some(md), reg))
                | None -> generics <- generics.Add(typeId, (at, Some(md), reg))
                          typeids <- typeids.Add(at.GetGenericTypeDefinition().AssemblyQualifiedName, typeId)
            | _ -> wrongQ()

        // Returns list of all known registrar method for types in this library
        member x.GetRegistrars () =

            // http://stackoverflow.com/questions/4168489/methodinfo-equality-for-declaring-type
            let areEqual (mb1 : MethodInfo)  (mb2 : MethodInfo) = 
                let first = if mb1.ReflectedType = mb1.DeclaringType then mb1
                            else mb1.DeclaringType.GetMethod(mb1.Name, mb1.GetParameters() |> Array.map(fun p -> p.ParameterType));
                let second = if mb2.ReflectedType = mb2.DeclaringType then mb2
                                else mb2.DeclaringType.GetMethod(mb2.Name, mb2.GetParameters() |> Array.map(fun p -> p.ParameterType));
                first = second

            serializers 
            |> Seq.choose (fun p -> match p.Value with
                                    | (_,_,None) -> None
                                    | (_,_,m) -> m)
            |> Seq.distinctBy(fun (mb : MethodBase) -> 
                                    if mb.ReflectedType = mb.DeclaringType then mb
                                    else mb.DeclaringType.GetMethod(mb.Name, mb.GetParameters() |> Array.map(fun p -> p.ParameterType)) :> MethodBase)
            |> Array.ofSeq

    static member CreateEmpty() = 
        SerializerLibrary("") :> ISerializerLibrary
    static member CreateDefault() = 
        SerializerCompositeLibrary("", SerializerLibrary(""), [ CoreSerializerResolver.Instance ] ) :> ISerializerLibrary

and SerializerCompositeLibrary(name, library : ISerializerLibrary, resolvers : ISerializerResolver seq) =
    let resolver = SerializerCompositeResolver((library :> ISerializerResolver) :: (List.ofSeq resolvers)) :> ISerializerResolver
    interface ISerializerLibrary with
        member x.Name = name
        member x.Register<'a>(t,s,d) = library.Register<'a>(t,s,d)
        member x.RegisterGeneric(t,q) = library.RegisterGeneric(t,q)
        member x.RegisterTransient(id,t) = library.RegisterTransient(id,t)
        member x.GetRegistrars() = library.GetRegistrars()
        member x.TryResolveType r t = resolver.TryResolveType r t
        member x.TryResolveTypeId r id = resolver.TryResolveTypeId r id
