namespace Angara.Serialization

type IBlob =   
    abstract member GetStream : unit -> System.IO.Stream
    abstract member WriteTo: System.IO.Stream -> unit

type InfoSet = 
    // Artefact is a content with type
    | Artefact of string * InfoSet
    // Sequence
    | Seq of seq<InfoSet> 
    // Mapping
    | Map of Map<string, InfoSet> 
    // Simple values (leafs)
    | Null
    | String of string 
    | Int of int 
    | UInt of System.UInt32
    | Int64 of System.Int64
    | UInt64 of System.UInt64
    | Decimal of decimal
    | Double of double 
    | Bool of bool
    | DateTime of System.DateTime
    | Guid of System.Guid
    // Arrays
    | BoolArray of bool seq
    | ByteArray of byte seq
    | IntArray of int seq
    | UIntArray of System.UInt32 seq
    | Int64Array of System.Int64 seq
    | UInt64Array of System.UInt64 seq
    | DecimalArray of System.Decimal seq
    | DoubleArray of double seq
    | DateTimeArray of System.DateTime seq
    | StringArray of string seq
    // Blobs
    | Namespace of string list * InfoSet
    | Blob of string * IBlob

    // Map constructions
    static member EmptyMap = InfoSet.Map(Map.empty<string,InfoSet>)

    member x.AddInfoSet (key, v)  = 
        match x with
        | Map(m) -> Map(m.Add(key, v))
        | _ -> failwith "Only map InfoSets support adding properties"

    member x.AddNull key = x.AddInfoSet(key, InfoSet.Null)
    member x.AddInt (key,i) = x.AddInfoSet(key, InfoSet.Int(i))
    member x.AddUInt (key,i) = x.AddInfoSet(key, InfoSet.UInt(i))
    member x.AddInt64 (key,i) = x.AddInfoSet(key, InfoSet.Int64(i))
    member x.AddUInt64 (key,i) = x.AddInfoSet(key, InfoSet.UInt64(i))
    member x.AddGuid (key,g) = x.AddInfoSet(key, InfoSet.Guid(g))
    member x.AddDouble (key,d) = x.AddInfoSet(key, InfoSet.Double(d))
    member x.AddDecimal (key,d) = x.AddInfoSet(key, InfoSet.Decimal(d))
    member x.AddBool (key,b) = x.AddInfoSet(key, InfoSet.Bool(b))
    member x.AddDateTime (key,d) = x.AddInfoSet(key, InfoSet.DateTime(d))
    member x.AddString (key,s) = x.AddInfoSet(key, InfoSet.String(s))

    member x.AddBlob (key,suffix,blob) = x.AddInfoSet(key, InfoSet.Blob(suffix, blob))
    member x.AddSeq (key, s) = x.AddInfoSet(key, InfoSet.Seq(s))

    // Map field access
    static member tryGetString key (map : Map<string, InfoSet>) =
        match Map.tryFind key map with
        | Some(s) -> Some(s.ToStringValue())
        | None -> None

    static member tryGetInt key (map : Map<string, InfoSet>) =
        match Map.tryFind key map with
        | Some(i) -> Some(i.ToInt())
        | None -> None
   
    static member tryGetIntArray key (map : Map<string, InfoSet>) =
        match Map.tryFind key map with
        | Some(i) -> Some(i.ToIntArray())
        | None -> None

    static member tryGetByteArray key (map : Map<string, InfoSet>) =
        match Map.tryFind key map with
        | Some(i) -> Some(i.ToByteArray())
        | None -> None

    static member tryGetStringArray key (map : Map<string, InfoSet>) =
        match Map.tryFind key map with
        | Some(i) -> Some(i.ToStringArray())
        | None -> None

    static member tryGetMap key (map : Map<string,InfoSet>) =
        match Map.tryFind key map with
        | Some(m) -> Some(m.ToMap())
        | None -> None

    static member tryGetSeq key (map : Map<string,InfoSet>) =
        match Map.tryFind key map with
        | Some(s) -> Some(s.ToSeq())
        | None -> None

    // Conversions to values.
    member x.ToStringValue() = match x with
                               | String(s) -> s
                               | Null -> null
                               | _ -> failwith "String value expected"

    member x.ToInt() = match x with
                       | Int(i) -> i
                       | Double(f) -> int(f)
                       | _ -> failwith "Numeric value expected"

    member x.ToUInt() = match x with
                        | UInt(i) -> i
                        | _ -> failwith "UInt (32-bit) value expected"

    member x.ToInt64() = match x with
                         | Int64(i) -> i
                         | Int(i) -> int64(i)
                         | _ -> failwith "Signed integer value expected"

    member x.ToUInt64() = match x with
                          | UInt64(i) -> i
                          | UInt(i) -> uint64(i)
                          | _ -> failwith "Unsigned integer value expected"

    member x.ToDecimal() = match x with
                           | Decimal(d) -> d
                           | _ -> failwith "Decimal value expected"

    member x.ToDouble() = match x with
                          | Int(i) -> float(i)
                          | Int64(i) -> float(i)
                          | UInt(i) -> float(i)
                          | UInt64(i) -> float(i)
                          | Double(f) -> f
                          | _ -> failwith "Numeric value expected"

    member x.ToBool() = match x with
                        | Bool(b) -> b
                        | _ -> failwith "Boolean value expected"

    member x.ToGuid() = match x with
                        | Guid(g) -> g
                        | _ -> failwith "Guid value expected"

    member x.ToDateTime() = match x with
                            | DateTime(dt) -> dt
                            | _ -> failwith "DateTime value expected"

    member x.ToIntArray() = match x with
                            | IntArray(a) -> a |> Array.ofSeq
                            | _ -> failwith "Array of integers expected"

    member x.ToUIntArray() = match x with
                             | UIntArray(a) -> a |> Array.ofSeq
                             | _ -> failwith "Array of unsigned integers expected"

    member x.ToInt64Array() = match x with
                              | Int64Array(a) -> a |> Array.ofSeq
                              | _ -> failwith "Array of integers expected"

    member x.ToUInt64Array() = match x with
                               | UInt64Array(a) -> a |> Array.ofSeq
                               | _ -> failwith "Array of unsigned integers expected"

    member x.ToBoolArray() = match x with
                             | BoolArray(a) -> a |> Array.ofSeq
                             | _ -> failwith "Array of booleans expected"
                             
    member x.ToByteArray() = match x with
                             | ByteArray(a) -> a |> Array.ofSeq
                             | _ -> failwith "Array of bytes expected"

    member x.ToDoubleArray() = match x with
                               | DoubleArray(a) -> a |> Array.ofSeq
                               | _ -> failwith "Array of floats expected"

    member x.ToDecimalArray() = match x with
                                | DecimalArray(a) -> a |> Array.ofSeq
                                | _ -> failwith "Array of floats expected"


    member x.ToDateTimeArray() = match x with
                                 | DateTimeArray(a) -> a |> Array.ofSeq
                                 | _ -> failwith "Array of DateTime expected"
    
    member x.ToStringArray() = match x with
                               | StringArray(a) -> a |> Array.ofSeq
                               | Seq(s) -> if Seq.isEmpty s then Array.empty<string>
                                                            else failwith "Array of strings or empty sequence expected"
                               | _ -> failwith "Array of strings expected"

    member x.ToNamespace() = match x with
                             | Namespace(names, infoSet) -> names, infoSet
                             | _ -> failwith "Namespace expected"

    member x.ToMap() = match x with
                       | Map(m) -> m
                       | _ -> failwith "Mapping expected"

    member x.ToPair() = let map = x.ToMap()
                        if map.Count = 1 then
                          Seq.head (Map.toSeq map)
                        else
                          failwith "Single-element map expected"

    member x.ToSeq() = match x with
                       | Seq(s) -> s
                       | Map(m) -> let n = m.Count
                                   seq { for i in 0..n-1 -> let key = i.ToString()
                                                            match m.TryFind key with
                                                            | Some v -> v
                                                            | None -> failwith (sprintf "Cannot use map as sequence - missing property %s" key) } 
                       | _ -> failwith "Sequence expected"

    member x.ToBlob() = match x with
                        | Blob(n,b) -> (n,b)
                        | _ -> failwith "Blob expected"
    
    static member ofPairs pairs = InfoSet.Map(Map.ofSeq pairs)

    static member toMap (si : InfoSet) = si.ToMap()

