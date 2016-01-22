namespace Angara.Serialization

open System
open System.Collections

type IntSerializer private () =
    interface ISerializer<int> with
        member x.TypeId = TypeIds.Int
        member x.Serialize _ i = InfoSet.Int(i)
        member x.Deserialize _ s = s.ToInt()
    static member Instance = IntSerializer() |> Helpers.MakeUntypedSerializer

type UIntSerializer private () =
    interface ISerializer<uint32> with
        member x.TypeId = TypeIds.UInt
        member x.Serialize _ i = InfoSet.UInt(i)
        member x.Deserialize _ s = s.ToUInt()
    static member Instance = UIntSerializer() |> Helpers.MakeUntypedSerializer

type Int64Serializer private () =
    interface ISerializer<int64> with
        member x.TypeId = TypeIds.Int64
        member x.Serialize _ i = InfoSet.Int64(i)
        member x.Deserialize _ s = s.ToInt64()
    static member Instance = Int64Serializer() |> Helpers.MakeUntypedSerializer

type UInt64Serializer private () =
    interface ISerializer<uint64> with
        member x.TypeId = TypeIds.UInt64
        member x.Serialize _ i = InfoSet.UInt64(i)
        member x.Deserialize _ s = s.ToUInt64()
    static member Instance = UInt64Serializer() |> Helpers.MakeUntypedSerializer

type Int16Serializer private () =
    interface ISerializer<int16> with
        member x.TypeId = TypeIds.Int16
        member x.Serialize _ i = InfoSet.Int(int(i))
        member x.Deserialize _ s = int16(s.ToInt())
    static member Instance = Int16Serializer() |> Helpers.MakeUntypedSerializer

type UInt16Serializer private () =
    interface ISerializer<uint16> with
        member x.TypeId = TypeIds.UInt16
        member x.Serialize _ i = InfoSet.Int(int(i))
        member x.Deserialize _ s = uint16(s.ToInt())
    static member Instance = UInt16Serializer() |> Helpers.MakeUntypedSerializer

type ByteSerializer private () =
    interface ISerializer<byte> with
        member x.TypeId = TypeIds.Byte
        member x.Serialize _ i = InfoSet.Int(int(i))
        member x.Deserialize _ s = byte(s.ToInt())
    static member Instance = ByteSerializer() |> Helpers.MakeUntypedSerializer

type DoubleSerializer private () =
    interface ISerializer<double> with
        member x.TypeId = TypeIds.Double
        member x.Serialize _ i = InfoSet.Double(i)
        member x.Deserialize _ s = s.ToDouble()
    static member Instance = DoubleSerializer() |> Helpers.MakeUntypedSerializer

type DecimalSerializer private () =
    interface ISerializer<decimal> with
        member x.TypeId = TypeIds.Decimal
        member x.Serialize _ i = InfoSet.Decimal(i)
        member x.Deserialize _ s = s.ToDecimal()
    static member Instance = DecimalSerializer() |> Helpers.MakeUntypedSerializer


type DateTimeSerializer private () =
    interface ISerializer<DateTime> with
        member x.TypeId = TypeIds.DateTime
        member x.Serialize _ i = InfoSet.DateTime(i)
        member x.Deserialize _ s = s.ToDateTime()
    static member Instance = DateTimeSerializer() |> Helpers.MakeUntypedSerializer

type BoolSerializer private () =
    interface ISerializer<bool> with
        member x.TypeId = TypeIds.Bool
        member x.Serialize _ b = InfoSet.Bool(b)
        member x.Deserialize _ s = s.ToBool()
    static member Instance = BoolSerializer() |> Helpers.MakeUntypedSerializer

type StringSerializer private () =
    interface ISerializer<string> with
        member x.TypeId = TypeIds.String
        member x.Serialize _ s = InfoSet.String(s)
        member x.Deserialize _ s = s.ToStringValue()
    static member Instance = StringSerializer() |> Helpers.MakeUntypedSerializer

type TupleSerializer (tupleType : Type, itemTypeIds : string list) =
    interface ISerializer with
        member x.Type = tupleType
        member x.TypeId = TypeId.ToString(TypeIds.Tuple, itemTypeIds)
        member x.Serialize r a = 
            Helpers.SerializeSequence r
                                      [ for i in 0..itemTypeIds.Length - 1 -> let mi = if i < 7 then tupleType.GetMethod(sprintf "get_Item%d" (i+1))
                                                                                                else tupleType.GetMethod(sprintf "get_Rest")
                                                                              mi.Invoke(a, [||]) ]
        member x.Deserialize r infoSet = 
            let items = Helpers.DeserializeSequence r infoSet |> Array.ofSeq 
            Activator.CreateInstance(tupleType, items)

    static member GetSerializer (itemTypes : Type list, itemTypeIds : string list) = 
        let itemTypes = itemTypes |> Array.ofList
        let tupleType = Type.GetType(sprintf "System.Tuple`%d" itemTypes.Length).MakeGenericType(itemTypes)
        TupleSerializer(tupleType, itemTypeIds)

    static member TryGetSerializer(res, itemTypeIds : TypeId list) =
        let itemTypeIds = itemTypeIds |> List.map (fun t -> t.ToString())
        match Helpers.TryGetTypes res itemTypeIds with
        | Choice1Of2(itemTypes) -> Serializable(TupleSerializer.GetSerializer(itemTypes, itemTypeIds))
        | Choice2Of2(t) -> NotFound(t)

    static member TryGetSerializer(res, itemTypes : Type list) =
        match Helpers.TryGetTypeIds res itemTypes with
        | Choice1Of2(itemTypeIds) -> Serializable(TupleSerializer.GetSerializer(itemTypes, itemTypeIds))
        | Choice2Of2(t) -> NotFound(t)

type ListSerializer<'T> (itemTypeId : string) = 
    interface ISerializer with
        member x.Type = typeof<'T list>
        member x.TypeId = TypeId.MakeList itemTypeId
        member x.Serialize r a = Helpers.SerializeSequence r (a :?> IEnumerable |> Seq.cast<obj>)
        member x.Deserialize r infoSet = 
            upcast(Helpers.DeserializeSequence r infoSet |> Seq.cast<'T> |> List.ofSeq)

    static member GetSerializer(itemType : Type, itemTypeId : string) =
        typedefof<ListSerializer<_>>
            .MakeGenericType([| itemType |])
            .GetConstructor([| typeof<string> |])
            .Invoke([| itemTypeId |]) :?> ISerializer

    static member TryGetSerializer(res, itemType : Type) =
        match itemType |> Helpers.TryGetTypeId res with
        | Choice1Of2 itemTypeId -> Serializable(ListSerializer<_>.GetSerializer(itemType, itemTypeId))
        | Choice2Of2 t -> NotFound(t)

    static member TryGetSerializer (res, itemTypeId : TypeId) = 
        let itemTypeId = itemTypeId.ToString()
        match itemTypeId |> Helpers.TryGetType res with
        | Choice1Of2 itemType -> Serializable(ListSerializer<_>.GetSerializer(itemType, itemTypeId))
        | Choice2Of2 t -> NotFound(t)
            
type ArraySerializer<'T> (itemTypeId : string) = 
    interface ISerializer with
        member x.Type = typeof<'T array>
        member x.TypeId = TypeId.MakeArray itemTypeId
        member x.Serialize r a = Helpers.SerializeSequence r (a :?> IEnumerable |> Seq.cast<obj>)
        member x.Deserialize r infoSet = upcast(Helpers.DeserializeSequence r infoSet |> Seq.cast<'T> |> Array.ofSeq)

    static member GetSerializer(itemType : Type, itemTypeId : string) =
        typedefof<ArraySerializer<_>>
            .MakeGenericType([| itemType |])
            .GetConstructor([| typeof<string> |])
            .Invoke([| itemTypeId |]) :?> ISerializer

    static member TryGetSerializer(res, itemType : Type) =
        match itemType |> Helpers.TryGetTypeId res with
        | Choice1Of2 itemTypeId -> Serializable(ArraySerializer<_>.GetSerializer(itemType, itemTypeId))
        | Choice2Of2 t -> NotFound(t)

    static member TryGetSerializer (res, itemTypeId : TypeId) = 
        let itemTypeId = itemTypeId.ToString()
        match itemTypeId |> Helpers.TryGetType res with
        | Choice1Of2 itemType -> Serializable(ArraySerializer<_>.GetSerializer(itemType, itemTypeId))
        | Choice2Of2 t -> NotFound(t)

type OptionSerializer<'T> (itemTypeId : string) =
    interface ISerializer with
        member x.Type = typeof<'T option>
        member x.TypeId = TypeId.MakeOption itemTypeId
        member x.Serialize r a = match a :?> 'T option with
                                 | Some(a) -> ArtefactSerializer.Serialize r a
                                 | None -> InfoSet.Null
        member x.Deserialize r infoSet = 
            match infoSet with
            | Null -> upcast None 
            | i -> upcast Some(ArtefactSerializer.Deserialize r i)

    static member GetSerializer(itemType : Type, itemTypeId : string) =
        typedefof<OptionSerializer<_>>
            .MakeGenericType([| itemType |])
            .GetConstructor([| typeof<string> |])
            .Invoke([| itemTypeId |]) :?> ISerializer

    static member TryGetSerializer(res, itemType : Type) =
        match itemType |> Helpers.TryGetTypeId res with
        | Choice1Of2 itemTypeId -> Serializable(OptionSerializer<_>.GetSerializer(itemType, itemTypeId))
        | Choice2Of2 t -> NotFound(t)

    static member TryGetSerializer (res, itemTypeId : TypeId) = 
        let itemTypeId = itemTypeId.ToString()
        match itemTypeId |> Helpers.TryGetType res with
        | Choice1Of2 itemType -> Serializable(OptionSerializer<_>.GetSerializer(itemType, itemTypeId))
        | Choice2Of2 t -> NotFound(t)

// Resolver for core type's serializers
type CoreSerializerResolver private () =
    interface ISerializerResolver with
        member x.TryResolveType lib t = 
            match Type.GetTypeCode(t) with
            // Primitives 
            | TypeCode.Byte -> Serializable(ByteSerializer.Instance)
            | TypeCode.Decimal -> Serializable(DecimalSerializer.Instance)
            | TypeCode.Int32 -> Serializable(IntSerializer.Instance)
            | TypeCode.UInt32 -> Serializable(UIntSerializer.Instance)
            | TypeCode.Int16 -> Serializable(Int16Serializer.Instance)
            | TypeCode.UInt16 -> Serializable(UInt16Serializer.Instance)
            | TypeCode.Int64 -> Serializable(Int64Serializer.Instance)
            | TypeCode.UInt64 -> Serializable(UInt64Serializer.Instance)
            | TypeCode.Double -> Serializable(DoubleSerializer.Instance)
            | TypeCode.String -> Serializable(StringSerializer.Instance)
            | TypeCode.Boolean -> Serializable(BoolSerializer.Instance)
            | TypeCode.DateTime -> Serializable(DateTimeSerializer.Instance)
            | TypeCode.Object -> 
                if Helpers.IsGenericList t then ListSerializer<_>.TryGetSerializer(lib, t.GenericTypeArguments.[0])
                elif Helpers.IsTuple t then TupleSerializer.TryGetSerializer(lib, t.GenericTypeArguments |> List.ofArray)
                elif t.IsArray && t.GetArrayRank() = 1 then ArraySerializer<_>.TryGetSerializer(lib, t.GetElementType())
                elif t = typeof<obj> then Transient(typeof<obj>, TypeIds.Object)
                elif Helpers.IsOption t then OptionSerializer<_>.TryGetSerializer(lib, t.GenericTypeArguments.[0])
                else NotFound(t.FullName)
            | _ -> NotFound(t.FullName)

        member x.TryResolveTypeId lib typeId =
            match typeId with
            | TypeIds.Byte -> Serializable(ByteSerializer.Instance)
            | TypeIds.Int -> Serializable(IntSerializer.Instance)
            | TypeIds.UInt -> Serializable(UIntSerializer.Instance)
            | TypeIds.Int64 -> Serializable(Int64Serializer.Instance)
            | TypeIds.UInt64 -> Serializable(UInt64Serializer.Instance)
            | TypeIds.Int16 -> Serializable(Int16Serializer.Instance)
            | TypeIds.UInt16 -> Serializable(UInt16Serializer.Instance)
            | TypeIds.Double -> Serializable(DoubleSerializer.Instance)
            | TypeIds.Bool -> Serializable(BoolSerializer.Instance)
            | TypeIds.String -> Serializable(StringSerializer.Instance)
            | TypeIds.DateTime -> Serializable(DateTimeSerializer.Instance)
            | TypeIds.Decimal -> Serializable(DecimalSerializer.Instance)
            | TypeIds.Object -> Transient(typeof<obj>, TypeIds.Object)
            | _ -> match TypeId.TryParse typeId with 
                   | Some(Generic(t,args)) when t = TypeIds.List && args.Length = 1 -> 
                        ListSerializer<_>.TryGetSerializer(lib, args.[0])
                   | Some(Generic(t,args)) when t = TypeIds.Tuple && args.Length >= 1 && args.Length <= 8 ->
                        TupleSerializer.TryGetSerializer(lib, args)
                   | Some(Generic(t, args)) when t = TypeIds.Array && args.Length = 1 ->
                        ArraySerializer<_>.TryGetSerializer(lib, args.[0])
                   | Some(Generic(t, args)) when t = TypeIds.Option && args.Length = 1 ->
                        OptionSerializer<_>.TryGetSerializer(lib, args.[0])
                   | _ -> NotFound(typeId)
    static member Instance = CoreSerializerResolver()