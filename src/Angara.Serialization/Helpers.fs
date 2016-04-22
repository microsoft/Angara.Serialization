namespace Angara.Serialization

[<Interface>]
type ISerializer<'T> = 
    abstract member TypeId : string
    abstract member Serialize : ISerializerResolver -> 'T -> InfoSet
    abstract member Deserialize : ISerializerResolver -> InfoSet -> 'T

module Helpers =
    open System

    let IsGenericList (t : Type) = t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<Microsoft.FSharp.Collections.List<_>>

    let IsOption (t : Type) = t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<_ option>

    let IsTuple (t : Type) = 
        t.IsGenericType &&
        let td = t.GetGenericTypeDefinition() in
        td = typedefof<Tuple<_>> ||
        td = typedefof<Tuple<_,_>> ||
        td = typedefof<Tuple<_,_,_>> ||
        td = typedefof<Tuple<_,_,_,_>> ||
        td = typedefof<Tuple<_,_,_,_,_>> ||
        td = typedefof<Tuple<_,_,_,_,_,_>> ||
        td = typedefof<Tuple<_,_,_,_,_,_,_>> ||
        td = typedefof<Tuple<_,_,_,_,_,_,_,_>>

    let MakeUntypedSerializer<'t> (s : ISerializer<'t>) =
        { new ISerializer with
              member x.Type = typeof<'t>
              member x.TypeId = s.TypeId
              member x.Serialize r a = s.Serialize r (a :?> 't)
              member x.Deserialize r infoSet = upcast(s.Deserialize r infoSet) }

    let internal appendSuffix (typeID : string option) suffix =
        match typeID with
        | Some("") -> suffix
        | Some(t) -> t + " " + suffix
        | None -> suffix

    let rec internal containsBlob (is : InfoSet) =
        match is with
        | Blob(_) -> true
        | Map(map) -> map |> Map.exists (fun _ v -> containsBlob(v))
        | Seq(seq) -> seq |> Seq.exists containsBlob
        | Artefact(_,infoSet) -> containsBlob infoSet
        | Namespace(_, infoSet) -> containsBlob infoSet
        | _ -> false

    let AddNamespace ns infoSet =
        if containsBlob infoSet then InfoSet.Namespace([ ns ], infoSet) else infoSet

    let SkipNamespace infoSet =
        match infoSet with
        | Namespace(_, content) -> content
        | _ -> infoSet

    let SerializeSequence<'T> res (items : obj seq) = 
        items |> Seq.mapi (fun index item -> let infoSet = ArtefactSerializer.Serialize res item
                                             AddNamespace (index.ToString()) infoSet)
              |> List.ofSeq |> Seq.ofList |> InfoSet.Seq

    let DeserializeSequence res (infoSet : InfoSet) = 
        infoSet.ToSeq() |> Seq.mapi(fun index infoSet -> 
                                        let noNS = SkipNamespace infoSet                          
                                        ArtefactSerializer.Deserialize res noNS)

    let TryGetTypeId (lib : ISerializerResolver) t =
        match lib.TryResolveType lib t with
        | Serializable(s) -> Choice1Of2 s.TypeId
        | Transient(_, typeId) -> Choice1Of2 typeId
        | NotFound t -> Choice2Of2 t

    let TryGetType (lib : ISerializerResolver) typeId =
        match lib.TryResolveTypeId lib typeId with
        | Serializable(s) -> Choice1Of2 s.Type
        | Transient(t,_) -> Choice1Of2 t
        | NotFound t -> Choice2Of2 t
       
    let TryGetTypeIds (res : ISerializerResolver) (types : Type seq) =
        let rec resolve (en : System.Collections.Generic.IEnumerator<Type>) =
            if en.MoveNext()
            then match en.Current |> TryGetTypeId res with
                 | Choice1Of2 t -> match resolve en with
                                   | Choice1Of2 tail -> Choice1Of2 (t :: tail)
                                   | e -> e
                 | Choice2Of2 t -> Choice2Of2 t
            else Choice1Of2 []
        use en = types.GetEnumerator()
        resolve en

    let TryGetTypes (res : ISerializerResolver) (typeIds : string seq) =
        let rec resolve (en : System.Collections.Generic.IEnumerator<string>) =
            if en.MoveNext()
            then match en.Current |> TryGetType res with
                 | Choice1Of2 t -> match resolve en with
                                   | Choice1Of2 tail -> Choice1Of2 (t :: tail)
                                   | e -> e
                 | Choice2Of2 t -> Choice2Of2 t
            else Choice1Of2 []
        use en = typeIds.GetEnumerator()
        resolve en