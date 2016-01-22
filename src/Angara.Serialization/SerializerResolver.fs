namespace Angara.Serialization

open System.Linq.Expressions
open System.Reflection
open System
open System.Collections.Concurrent

[<Interface>]
[<AllowNullLiteral>]
type ISerializer = 
    abstract member Type : Type
    abstract member TypeId : string
    abstract member Serialize : ISerializerResolver -> obj -> InfoSet 
    abstract member Deserialize : ISerializerResolver -> InfoSet -> obj 

and ResolveResult =
    | Serializable of ISerializer
    | Transient of Type * string // An object of a non-serializable type
    | NotFound of string // Full name of CLR type or TypeId causing problems

and [<Interface>] 
    ISerializerResolver = 
    abstract member TryResolveType : ISerializerResolver -> Type -> ResolveResult
    abstract member TryResolveTypeId : ISerializerResolver -> string -> ResolveResult