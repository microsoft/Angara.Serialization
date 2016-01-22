namespace Angara.Serialization

type CustomSerializerResolver () =
    let ext = SerializerLibrary()
    let res = SerializerCompositeResolver([ ext; CoreSerializerResolver.Instance ]) :> ISerializerResolver

    interface ISerializerResolver with
        member x.TryResolveType r t = res.TryResolveType r t
        member x.TryResolveTypeId r t = res.TryResolveTypeId r t

    member x.Library = ext
        
