namespace Angara.Serialization

open System
open System.Reflection
open System.Diagnostics

[<Interface>]
type ISerializerLibrary =
    inherit ISerializerResolver
    abstract Name: string with get
    abstract Register<'a> : ISerializer<'a> -> unit
    abstract RegisterTransient: string * Type -> unit
    abstract GetRegistrars: unit -> MethodBase[]

type SerializerLibrary (name: string) =

    // TypeId -> Artefact type * Serializer instance (null for transient) * Registrar method (maybe None)
    let mutable serializers = Map.empty<string, Type * ISerializer * MethodBase option>     
    // Assembly qualified CLR type -> TypeId
    let mutable typeids = Map.empty<string, string> // 
    
    let getRegistrar (s : StackTrace) =
        let chooser (frame : StackFrame) = 
            let m = frame.GetMethod()
            if m.IsStatic 
            then let p = m.GetParameters()
                 if p.Length = 1 && p.[0].ParameterType = typeof<ISerializerLibrary seq> then Some(m) else None
            else None
        s.GetFrames() |> Array.tryPick chooser
        
    let registerInstance typeId atype so (reg : MethodBase option) =
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

    interface ISerializerLibrary with
        member x.TryResolveType root t =
            let t = if t.BaseType <> null && t.BaseType = t.DeclaringType then t.BaseType else t // Special case for enums
            match typeids.TryFind(t.AssemblyQualifiedName) with
            | Some(typeId) -> match serializers.TryFind typeId with
                              | None -> NotFound (t.FullName)
                              | Some(t, null, _) -> Transient(t, typeId)
                              | Some(_, so, _) -> Serializable(so)
            | None -> NotFound (t.FullName)
        member x.TryResolveTypeId root typeId =
            match serializers.TryFind typeId with
            | None -> NotFound typeId
            | Some(t, null, _) -> Transient(t, typeId)
            | Some(_, so, _) -> Serializable(so)

        member x.Name = name

        member x.Register<'a>(s : ISerializer<'a>) = 
            let trace = StackTrace()
            registerInstance s.TypeId typeof<'a> (Angara.Serialization.Helpers.MakeUntypedSerializer s) (getRegistrar trace)
    
        member x.RegisterTransient(typeId : string, a : Type) = 
            let trace = StackTrace()
            registerInstance typeId a null (getRegistrar trace)

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
        member x.Register<'a>(s) = library.Register<'a>(s)
        member x.RegisterTransient(id,t) = library.RegisterTransient(id,t)
        member x.GetRegistrars() = library.GetRegistrars()
        member x.TryResolveType r t = resolver.TryResolveType r t
        member x.TryResolveTypeId r id = resolver.TryResolveTypeId r id
