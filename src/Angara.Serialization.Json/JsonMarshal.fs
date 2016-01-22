module Angara.Serialization.Json

open Angara.Serialization

open System
open Newtonsoft.Json
open Newtonsoft.Json.Linq

type IBlobWriter =
    abstract member AddGroup: string -> IBlobWriter
    abstract member Write: string * Angara.Serialization.IBlob -> unit

type IBlobReader =
    abstract member Read: string -> Angara.Serialization.IBlob
    abstract member GetGroup: string -> IBlobReader

type internal InlineBase64Blob(s : string) =
    interface IBlob with
        member x.GetStream() =
             let bytes = Convert.FromBase64String(s)
             let ms = new System.IO.MemoryStream()
             ms.Write(bytes, 0, bytes.Length)
             ms.Position <- 0L
             upcast ms
        member x.WriteTo stream = 
            let bytes = Convert.FromBase64String(s)
            stream.Write(bytes, 0, bytes.Length)

let internal encodeDecimalArray (a : System.Decimal array) = 
    let len = a.Length
    let buffer = Array.zeroCreate<byte>(len * 16)
    a |> Array.iteri(fun i d -> let bits = Decimal.GetBits d
                                Buffer.BlockCopy(bits, 0, buffer, i * 16, 16))
    Convert.ToBase64String(buffer)

let internal decodeDecimalArray s = 
    let buffer = Convert.FromBase64String(s)
    let length = buffer.Length / 16
    let bits = Array.zeroCreate<int> 4
    [| for i in 0..length - 1 -> Buffer.BlockCopy(buffer, i * 16, bits, 0, 16)
                                 Decimal(bits) |]


let internal encode1DArray a = 
    let len = Buffer.ByteLength(a)
    let buffer = Array.zeroCreate<byte> len
    Buffer.BlockCopy(a, 0, buffer, 0, len)
    Convert.ToBase64String(buffer)

let internal decode1DArray<'a> (s, elsize) = 
    let buffer = Convert.FromBase64String(s)
    let length = buffer.Length / elsize
    let array = Array.zeroCreate<'a> length
    Buffer.BlockCopy(buffer, 0, array, 0, buffer.Length);
    array
    
let internal encodeNameAndType (n : string, t : string option) = 
    let n = n.Replace(":", "::")
    match t with
    | None -> n
    | Some(t) -> String.Concat(n, ":", t)

let internal decodeNameAndType (name : string) = 
    let idx = name.LastIndexOf(':');
    if idx = 0 || idx > 0 && name.[idx - 1] <> ':'
    then name.Substring(0, idx).Replace("::", ":"), Some(name.Substring(idx + 1))
    else name.Replace("::", ":"), None

let internal UnixEpochOrigin = DateTime(1970,1,1,0,0,0, DateTimeKind.Utc)
let internal DateTimeMinUtc = DateTime.MinValue.ToUniversalTime()
let internal DateTimeMaxUtc = DateTime.MaxValue.ToUniversalTime()

/// Returns the number of milliseconds since 1 Jan 1970 00:00:00 UTC (Unix Epoch)
let internal DateTimeToUnixEpoch (dt:DateTime) : float =
    let udt = if dt = DateTime.MinValue then DateTimeMinUtc
              elif dt = DateTime.MaxValue then DateTimeMaxUtc 
              else dt.ToUniversalTime()
    udt.Subtract(UnixEpochOrigin).TotalMilliseconds

/// Returns the DateTime instance (local time) from number of milliseconds since 1 Jan 1970 00:00:00 UTC (Unix Epoch)
let internal UnixEpochToDateTime (value: float) : DateTime =
    let udt = UnixEpochOrigin.AddMilliseconds(value)
    if udt = DateTimeMinUtc then DateTime.MinValue
    elif udt = DateTimeMaxUtc then DateTime.MaxValue
    else udt.ToLocalTime()

let rec Marshal (infoSet, writer : IBlobWriter option) = 

    let rec getJson (is : InfoSet, writer : IBlobWriter option) : (JToken * string option) =

        let encodeMap (map : Map<string, InfoSet>) =   
            let result = JObject()
            Map.iter (fun k v -> let jtoken, t = getJson(v,writer) 
                                 result.Add(encodeNameAndType(k, t), jtoken)) map
            result :> JToken

        match is with
        // Primitives
        | Null -> upcast JValue(null :> obj), None
        | Int(i) -> upcast JValue(i), Some("int")
        | UInt(i) -> upcast JValue(i), Some("uint")
        | Int64(i) -> upcast JValue(i), Some("int64")
        | UInt64(i) -> upcast JValue(Convert.ToBase64String(BitConverter.GetBytes(i))), Some("uint64") // JSON cannot represent entire range of UInt64 (see http://stackoverflow.com/questions/9355091/json-net-crashes-when-serializing-unsigned-integer-ulong-array)
        | Decimal(d) -> let bytes = Decimal.GetBits(d) |> Array.map BitConverter.GetBytes |> Array.concat
                        upcast JValue(Convert.ToBase64String(bytes)), Some("decimal")
        | Double(d) -> if Double.IsNaN d || Double.IsInfinity d
                       then upcast JValue(d), Some("double")
                       else upcast JValue(d), None
        | String(s) -> upcast JValue(s), if s = null then Some("string") else None         
        | Bool(b) -> upcast JValue(b), None 
        | DateTime(dt) -> upcast JValue(dt), Some("datetime")
        | Guid(g) -> upcast JValue(g.ToString()), Some("guid")
        // Arrays of primitives
        | IntArray(a) -> upcast JValue(a |> Array.ofSeq |> encode1DArray), Some("int array")
        | UIntArray(a) -> upcast JValue(a |> Array.ofSeq |> encode1DArray), Some("uint array")
        | Int64Array(a) -> upcast JValue(a |> Array.ofSeq |> encode1DArray), Some("int64 array")
        | UInt64Array(a) -> upcast JValue(a |> Array.ofSeq |> encode1DArray), Some("uint64 array")
        | DoubleArray(a) -> upcast JValue(a |> Array.ofSeq |> encode1DArray), Some("double array")
        | DecimalArray(a) -> upcast JValue(a |> Array.ofSeq |> encodeDecimalArray), Some("decimal array")
        | BoolArray(a) -> upcast JValue(a |> Array.ofSeq |> encode1DArray), Some("bool array")
        | DateTimeArray(a) -> upcast JValue(a |> Seq.map DateTimeToUnixEpoch |> Array.ofSeq |> encode1DArray), Some("datetime array")
        | StringArray(a) -> upcast JArray(a |> Array.ofSeq), if Seq.isEmpty a then Some("string array") else None
        | ByteArray(a) -> upcast JValue(a |> Array.ofSeq |> encode1DArray), Some("byte array")
        // Mapping
        | Artefact(typeID, content) -> Marshal(content, writer), Some(typeID) 
        | Map(map) -> encodeMap(map), None
        // Sequence
        | Seq(s) -> let items = s |> Seq.map (fun i -> Marshal(i, writer)) 
                    upcast JArray(items), None
        // Blobs and namespaces
        | Namespace(names, infoSet) -> let result = JObject()
                                       result.Add("name", JArray(names))
                                       let nsc = getJson(infoSet, match writer with
                                                                  | None -> None
                                                                  | Some(origin) -> let mutable w = origin
                                                                                    for n in names do
                                                                                        w <- w.AddGroup(n)
                                                                                    Some(w))
                                       result.Add(encodeNameAndType("content", snd nsc), fst nsc)
                                       upcast result, Some("namespace")
        | Blob(name,blob) -> let result = JObject() 
                             result.Add("name", JValue(name))
                             match writer with
                             | Some(w) -> w.Write(name, blob)                                      
                             | None -> use reader = new System.IO.BinaryReader(blob.GetStream())
                                       result.Add("data", JValue(System.Convert.ToBase64String(reader.ReadBytes(int(reader.BaseStream.Length)))))
                             upcast result, Some("data")

    match getJson(infoSet, writer) with
    | token, None -> token
    | token, Some(typeID) -> let res = JObject()
                             res.Add(encodeNameAndType("", Some(typeID)), token)
                             res :> JToken

let rec Unmarshal(token, reader) = 

    let rec getInfoSet (token : JToken, t : string option, reader : IBlobReader option) : InfoSet =

        let decodeMap (obj : JObject) =
            let mutable nts : (string * string option * InfoSet) list = List.empty
            for pair in obj do 
                let n,t = decodeNameAndType pair.Key
                nts <- (n,t,getInfoSet(pair.Value, t, reader)) :: nts
            if nts.Length = 1 
            then
                match nts.Head with
                | "", _, infoSet -> infoSet
                | n, _, infoSet  -> InfoSet.EmptyMap.AddInfoSet(n,infoSet)
            else
                nts |> List.map (fun t -> let n,_,s = t in n,s) |> InfoSet.ofPairs 

        match t with
        | None -> match token.Type with
                  | JTokenType.Null -> InfoSet.Null
                  | JTokenType.Integer -> InfoSet.Int(token.Value<int>())
                  | JTokenType.Float -> InfoSet.Double(token.Value<float>())
                  | JTokenType.String -> InfoSet.String(token.Value<string>())
                  | JTokenType.Boolean -> InfoSet.Bool(token.Value<bool>())
                  | JTokenType.Array -> 
                      let arr = token :?> JArray
                      if arr.Count > 0 && arr |> Seq.forall (fun i -> i.Type = JTokenType.String) 
                      then InfoSet.StringArray(arr |> Seq.map (fun i -> i.Value<string>()))
                      else InfoSet.Seq(arr |> Seq.map (fun i -> Unmarshal(i, reader)) |> List.ofSeq)
                  | JTokenType.Object -> token :?> JObject |> decodeMap
                  | _ -> failwith ("Cannot create InfoSet from JToken of type " + token.Type.ToString())
        // Primitives
        | Some("int") -> InfoSet.Int(token.Value<int>())
        | Some("uint") -> InfoSet.UInt(token.Value<UInt32>())
        | Some("int64") -> InfoSet.Int64(token.Value<Int64>())
        | Some("uint64") -> InfoSet.UInt64(if token.Type = JTokenType.String then BitConverter.ToUInt64(Convert.FromBase64String(token.Value<string>()), 0) 
                                                                             else token.Value<UInt64>())
        | Some("decimal") -> InfoSet.Decimal(if token.Type = JTokenType.String then let bytes = Convert.FromBase64String(token.Value<string>())
                                                                                    let ints = [|0..3|] |> Array.map (fun i -> BitConverter.ToInt32(bytes, i * 4))
                                                                                    Decimal(ints)
                                                                               else token.Value<decimal>())
        | Some("guid") -> InfoSet.Guid(Guid.Parse(token.Value<string>()))
        | Some("datetime") -> InfoSet.DateTime(token.Value<DateTime>())
        | Some("double") -> InfoSet.Double(token.Value<double>())
        | Some("string") -> InfoSet.String(token.Value<string>())
        // Arrays
        | Some("string array") -> InfoSet.StringArray(token.Values<string>())
        | Some("int array") -> InfoSet.IntArray(decode1DArray(token.Value<string>(), 4))
        | Some("uint array") -> InfoSet.UIntArray(decode1DArray(token.Value<string>(), 4))
        | Some("int64 array") -> InfoSet.Int64Array(decode1DArray(token.Value<string>(), 8))
        | Some("uint64 array") -> InfoSet.UInt64Array(decode1DArray(token.Value<string>(), 8))
        | Some("double array") -> InfoSet.DoubleArray(decode1DArray(token.Value<string>(), 8))
        | Some("decimal array") -> InfoSet.DecimalArray(decodeDecimalArray(token.Value<string>()))
        | Some("datetime array") -> InfoSet.DateTimeArray(decode1DArray<float>(token.Value<string>(), 8) |> Array.map UnixEpochToDateTime)
        | Some("bool array") -> InfoSet.BoolArray(decode1DArray(token.Value<string>(), 1))
        | Some("byte array") -> InfoSet.ByteArray(decode1DArray(token.Value<string>(), 1))
        // Blobs
        | Some("data") -> let obj = token :?> JObject
                          let name = obj.["name"].Value<string>()
                          InfoSet.Blob(name, match obj.TryGetValue("data") with
                                             | true, data -> InlineBase64Blob(data.Value<string>()) :> IBlob
                                             | false, _ -> match reader with
                                                                 | Some(r) -> r.Read(name)
                                                                 | None -> failwith ("Cannot read blob " + name + ". Reader is not supplied"))
        | Some("namespace") -> let obj = token :?> JObject
                               let mutable names = List.empty<string>
                               let mutable token : JToken = null
                               let mutable typeID : string option = None
                               for pair in obj do
                                   match decodeNameAndType pair.Key with
                                   | "content", t -> token <- pair.Value 
                                                     typeID <- t
                                   | "name", None -> names <- List.ofSeq(pair.Value.Values<string>())
                                   | n,_ -> failwith ("Cannot decode namespace record. Unknown field " + n)
                           
                               InfoSet.Namespace(names, getInfoSet(token, typeID, match reader with
                                                                                  | None -> None
                                                                                  | Some(origin) -> 
                                                                                       let mutable r = origin
                                                                                       for n in names do
                                                                                           r <- r.GetGroup(n)
                                                                                       Some(r)))
        // Generic type
        | Some(t) -> Artefact(t, match token with
                                 | :? JObject as obj -> decodeMap obj
                                 | :? JArray as arr -> InfoSet.Seq(arr |> Seq.map (fun i -> getInfoSet(i, None, reader)))
                                 | token -> Unmarshal(token, reader))

    let infoSet = getInfoSet(token, None, reader)
    match infoSet with
    | Map(map) when map.Count = 1 -> let pair = (map |> Map.toArray).[0]
                                     match fst pair |> decodeNameAndType with
                                     | "", Some(typeID) -> InfoSet.Artefact(typeID, snd pair)
                                     | _ -> infoSet
    | infoSet -> infoSet

