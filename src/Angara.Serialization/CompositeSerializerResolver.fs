namespace Angara.Serialization

type SerializerCompositeResolver (res : ISerializerResolver seq) =
    let resolvers = List.ofSeq res

    let pickFromLibraries name (picker: ISerializerResolver -> ResolveResult) =
        match resolvers |> Seq.tryPick(fun lib -> match picker lib with
                                                  | Serializable(s) -> Some(Serializable(s))
                                                  | Transient(t,i) -> Some(Transient(t,i))
                                                  | NotFound _ -> None) with
        | Some(found) -> found
        | None -> NotFound(name)

    interface ISerializerResolver with
        member x.TryResolveType rootLib t = pickFromLibraries (t.FullName) (fun lib -> lib.TryResolveType rootLib t)            
        member x.TryResolveTypeId rootLib typeId = pickFromLibraries typeId (fun lib -> lib.TryResolveTypeId rootLib typeId)



