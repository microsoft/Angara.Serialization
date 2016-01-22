namespace Angara.Serialization

open Angara.Serialization
open System
open System.Collections

type SerializerNotFoundException(t : Type) =
    inherit Exception(sprintf "Serializer for type %s is not found or type is transient" t.FullName)
    member x.Type = t

type DeserializerNotFoundException(typeId : string) =
    inherit Exception(sprintf "Deserializer for TypeId %s is not found or TypeId is transient" typeId)
    member x.TypeId = typeId

module ArtefactSerializer =
    
    let Serialize (res : ISerializerResolver) (a : obj) = 
        match a  with
        | null -> InfoSet.Null
        | _ -> let t = a.GetType()
               match Type.GetTypeCode(t) with 
               | TypeCode.Byte -> InfoSet.Artefact("byte", InfoSet.Int(int(a :?> byte)))
               | TypeCode.Int16 -> InfoSet.Artefact("int16", InfoSet.Int(int(a :?> int16)))
               | TypeCode.UInt16 -> InfoSet.Artefact("uint16", InfoSet.Int(int(a :?> uint16)))
               | TypeCode.Int32 -> InfoSet.Int(a :?> int)
               | TypeCode.UInt32 -> InfoSet.UInt(a :?> uint32)
               | TypeCode.Int64 -> InfoSet.Int64(a :?> int64)
               | TypeCode.UInt64 -> InfoSet.UInt64(a :?> uint64)
               | TypeCode.Double -> InfoSet.Double(a :?> double)
               | TypeCode.Boolean -> InfoSet.Bool(a :?> bool)
               | TypeCode.String -> InfoSet.String(a :?> string)
               | TypeCode.DateTime -> InfoSet.DateTime(a :?> DateTime)
               | TypeCode.Decimal -> InfoSet.Decimal(a :?> Decimal)
               | TypeCode.Object ->
                   match a with
                   | :? (int[]) -> InfoSet.IntArray(a :?> int[])
                   | :? (uint32[]) -> InfoSet.UIntArray(a :?> uint32[])
                   | :? (int64[]) -> InfoSet.Int64Array(a :?> int64[])
                   | :? (uint64[]) -> InfoSet.UInt64Array(a :?> uint64[])
                   | :? (double[]) -> InfoSet.DoubleArray(a :?> double[])
                   | :? (bool[]) -> InfoSet.BoolArray(a :?> bool[])
                   | :? (DateTime[]) -> InfoSet.DateTimeArray(a :?> DateTime[])
                   | :? (byte[]) -> InfoSet.ByteArray(a :?> byte[])
                   | :? (decimal[]) -> InfoSet.DecimalArray(a :?> decimal[])
                   | :? (string[]) -> InfoSet.StringArray(a :?> string[])
                   | :? Guid -> InfoSet.Guid(a :?> Guid)
                   | _ -> match res.TryResolveType res t with
                          | Serializable(s) -> InfoSet.Artefact(s.TypeId, s.Serialize res a)
                          | _ -> raise(SerializerNotFoundException(t))
               | _ -> raise(SerializerNotFoundException(t))

    let Deserialize (res : ISerializerResolver) (i : InfoSet) : obj =
        match i with
        // Primitives
        | InfoSet.Null -> upcast null
        | InfoSet.Int(i) -> upcast i
        | InfoSet.UInt(i) -> upcast i
        | InfoSet.Int64(i) -> upcast i
        | InfoSet.UInt64(i) -> upcast i
        | InfoSet.Double(d) -> upcast d
        | InfoSet.Bool(b) -> upcast b 
        | InfoSet.DateTime(dt) -> upcast dt
        | InfoSet.String(s) -> upcast s
        | InfoSet.Guid(g) -> upcast g
        | InfoSet.Decimal(d) -> upcast d
        // Arrays
        | InfoSet.IntArray(ia) -> upcast ia
        | InfoSet.UIntArray(uia) -> upcast uia
        | InfoSet.Int64Array(ia64) -> upcast ia64
        | InfoSet.UInt64Array(uia64) -> upcast uia64
        | InfoSet.DoubleArray(da) -> upcast da
        | InfoSet.BoolArray(ba) -> upcast ba
        | InfoSet.DateTimeArray(dta) -> upcast dta
        | InfoSet.ByteArray(ba) -> upcast ba
        | InfoSet.DecimalArray(dta) -> upcast dta
        | InfoSet.StringArray(sa) -> upcast sa
        // Composites
        | InfoSet.Artefact(typeId, content) -> match res.TryResolveTypeId res typeId with
                                               | Serializable(s) -> s.Deserialize res content
                                               | _ -> raise(DeserializerNotFoundException(typeId))
        // Not expected at this level
        | InfoSet.Blob(_,_) -> failwith "InfoSet.Blob is not expected at this level"
        | InfoSet.Namespace(_,_) -> failwith "InfoSet.Namespace is not expected at this level"
        | InfoSet.Map(_) -> failwith "InfoSet.Map is not expected at this level"
        | InfoSet.Seq(_) -> failwith "InfoSet.Seq is not expected at this level"