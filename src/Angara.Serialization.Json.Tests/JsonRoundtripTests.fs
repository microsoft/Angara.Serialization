module JsonRoundtripTests

open NUnit.Framework
open Angara.Serialization
open Swensen.Unquote
open System

open Angara.Serialization

let equalSeq<'U when 'U : comparison> (s1 : 'U seq) (s2 : 'U seq) =
    Seq.zip s1 s2 |> Seq.forall (fun (e1,e2) -> e1 = e2)

let shouldRoundtrip (i : InfoSet) =
    let json = Json.Marshal(i, None)
    let s = json.ToString(Newtonsoft.Json.Formatting.Indented)
    let json' = Newtonsoft.Json.Linq.JToken.Parse(s)
    let i' = Json.Unmarshal(json', None)
    i =! i'

let shouldSatisfy (p: InfoSet -> bool) (i : InfoSet) =
    let json = Json.Marshal(i, None)
    let s = json.ToString(Newtonsoft.Json.Formatting.Indented)
    let json' = Newtonsoft.Json.Linq.JToken.Parse(s)
    let i' = Json.Unmarshal(json', None)
    Assert.IsTrue(p i')

[<Test>]
let ``Primitive InfoSets roundtrip to JSON`` () =
    // Int32
    InfoSet.Int(Int32.MinValue) |> shouldRoundtrip
    InfoSet.Int(Int32.MaxValue) |> shouldRoundtrip

    // UInt32
    InfoSet.UInt(UInt32.MinValue) |> shouldRoundtrip
    InfoSet.UInt(UInt32.MaxValue) |> shouldRoundtrip

    // Int64
    InfoSet.Int64(Int64.MinValue) |> shouldRoundtrip
    InfoSet.Int64(Int64.MaxValue) |> shouldRoundtrip

    // UInt64
    InfoSet.UInt64(UInt64.MinValue) |> shouldRoundtrip
    InfoSet.UInt64(UInt64.MaxValue) |> shouldRoundtrip

    // Bool
    InfoSet.Bool(true) |> shouldRoundtrip
    InfoSet.Bool(false) |> shouldRoundtrip

    // Double
    InfoSet.Double(Double.MinValue) |> shouldRoundtrip
    InfoSet.Double(Double.MaxValue) |> shouldRoundtrip
    InfoSet.Double(Double.NaN) |> shouldSatisfy (function
                                                 | InfoSet.Double(d) -> Double.IsNaN d
                                                 | _ -> false)
    InfoSet.Double(Double.NegativeInfinity) |> shouldRoundtrip
    InfoSet.Double(Double.PositiveInfinity) |> shouldRoundtrip
    InfoSet.Double(0.84551240822557006) |> shouldRoundtrip

    // Decimal
    InfoSet.Decimal(Decimal.MinValue) |> shouldRoundtrip
    InfoSet.Decimal(Decimal.MaxValue) |> shouldRoundtrip

    // DateTime
    let dateTime = new DateTime(2016,1,11,19,07,21,525)
    InfoSet.DateTime(dateTime) |> shouldRoundtrip
    InfoSet.DateTime(DateTime.MinValue) |> shouldRoundtrip
    InfoSet.DateTime(DateTime.MaxValue) |> shouldRoundtrip

    // Guid
    let guid = System.Guid("D5A1A767-DCE9-4C1D-B946-D1970F2A153B")
    InfoSet.Guid(guid) |> shouldRoundtrip

    // String
    InfoSet.String("") |> shouldRoundtrip
    InfoSet.String(null) |> shouldRoundtrip
    InfoSet.String("Hello") |> shouldRoundtrip

[<Test>]
let ``Array InfoSets roundtrip to JSON`` () =
    // String arrays
    InfoSet.StringArray([ ]) |> shouldSatisfy (function
                                               | InfoSet.StringArray(s) -> Seq.isEmpty s
                                               | _ -> false)
    let sa = [ "Hello"; "World"; "!" ]
    InfoSet.StringArray(sa) |> shouldSatisfy (function
                                              | InfoSet.StringArray(s) -> equalSeq s sa
                                              | _ -> false)

    // Int32 arrays
    InfoSet.IntArray([||]) |> shouldSatisfy (function
                                             | InfoSet.IntArray(s) -> Seq.isEmpty s
                                             | _ -> false)
    let ia = [ Int32.MinValue; Int32.MaxValue ]
    InfoSet.IntArray(ia) |> shouldSatisfy (function
                                           | InfoSet.IntArray(s) -> equalSeq s ia
                                           | _ -> false)

    // UInt32 arrays
    InfoSet.UIntArray([||]) |> shouldSatisfy (function
                                              | InfoSet.UIntArray(s) -> Seq.isEmpty s
                                              | _ -> false)
    let uia = [ UInt32.MinValue; UInt32.MaxValue ]
    InfoSet.UIntArray(uia) |> shouldSatisfy (function
                                            | InfoSet.UIntArray(s) -> equalSeq s uia
                                            | _ -> false)

    // Int64 arrays
    InfoSet.Int64Array([||]) |> shouldSatisfy (function
                                               | InfoSet.Int64Array(s) -> Seq.isEmpty s
                                               | _ -> false)
    let ia64 = [ Int64.MinValue; Int64.MaxValue ]
    InfoSet.Int64Array(ia64) |> shouldSatisfy (function
                                               | InfoSet.Int64Array(s) -> equalSeq s ia64
                                               | _ -> false)

    // UInt64 arrays
    InfoSet.UInt64Array([||]) |> shouldSatisfy (function
                                               | InfoSet.UInt64Array(s) -> Seq.isEmpty s
                                               | _ -> false)
    let uia64 = [ UInt64.MinValue; UInt64.MaxValue ]
    InfoSet.UInt64Array(uia64) |> shouldSatisfy (function
                                                 | InfoSet.UInt64Array(s) -> equalSeq s uia64
                                                 | _ -> false)

    // Byte arrays
    InfoSet.ByteArray([||]) |> shouldSatisfy (function
                                              | InfoSet.ByteArray(s) -> Seq.isEmpty s
                                              | _ -> false)
    let ba = [ Byte.MinValue; Byte.MaxValue ]
    InfoSet.ByteArray(ba) |> shouldSatisfy (function
                                            | InfoSet.ByteArray(s) -> equalSeq s ba
                                            | _ -> false)

    // Double arrays
    InfoSet.DoubleArray([||]) |> shouldSatisfy (function
                                                | InfoSet.DoubleArray(s) -> Seq.isEmpty s
                                                | _ -> false)
    let da = [ Double.MaxValue; Double.NaN; Double.MinValue; 0.84551240822557006; Double.PositiveInfinity; Double.NegativeInfinity ]
    InfoSet.DoubleArray(da) |> shouldSatisfy (function
                                              | InfoSet.DoubleArray(s) -> let da' = s |> Array.ofSeq
                                                                          da'.[0] = da.[0] && 
                                                                          Double.IsNaN da'.[1] &&
                                                                          da'.[2] = da.[2] &&
                                                                          da'.[3] = da.[3] &&
                                                                          Double.IsPositiveInfinity da'.[4] &&
                                                                          Double.IsNegativeInfinity da'.[5]
                                              | _ -> false)

    // Bool arrays
    InfoSet.BoolArray([||]) |> shouldSatisfy (function
                                              | InfoSet.BoolArray(s) -> Seq.isEmpty s
                                              | _ -> false)
    let la = [ true; false; ]
    InfoSet.BoolArray(la) |> shouldSatisfy (function
                                            | InfoSet.BoolArray(s) -> equalSeq s la
                                            | _ -> false)

    // Decimal arrays
    InfoSet.DecimalArray([||]) |> shouldSatisfy (function
                                                 | InfoSet.DecimalArray(s) -> Seq.isEmpty s
                                                 | _ -> false)
    let dda = [ Decimal.MinValue; Decimal.MaxValue ]
    InfoSet.DecimalArray(dda) |> shouldSatisfy (function
                                                | InfoSet.DecimalArray(s) -> equalSeq s dda
                                                | _ -> false)

    // DateTime arrays
    InfoSet.DateTimeArray([||]) |> shouldSatisfy (function
                                                  | InfoSet.DateTimeArray(s) -> Seq.isEmpty s
                                                  | _ -> false)
    let dta = [ DateTime.MinValue; new DateTime(2016,1,12,11,52,48,733); DateTime.MaxValue ]
    InfoSet.DateTimeArray(dta) |> shouldSatisfy (function
                                                 | InfoSet.DateTimeArray(s) -> equalSeq s dta
                                                 | _ -> false)


type internal TestBlob (TestBlobContent) = 
    interface IBlob with
        member x.WriteTo stream = stream.Write(TestBlobContent, 0, TestBlobContent.Length)
        member x.GetStream() = upcast new System.IO.MemoryStream(TestBlobContent, false)
        
[<Test>]
let ``Blob and namespace rountrip to JSON (inlined)`` () = 
    let TestBlobContent = [| 0uy; 0xA0uy; 0xFFuy |]
    let ns = ["nested"; "namespaces"]
    let is = InfoSet.Namespace(ns, InfoSet.Blob("bin", TestBlob(TestBlobContent)))
    is |> shouldSatisfy (function 
                         | InfoSet.Namespace(n, InfoSet.Blob("bin", b)) -> let buffer = Array.zeroCreate<byte> 3
                                                                           use reader = new System.IO.BinaryReader(b.GetStream())
                                                                           n = ns &&
                                                                           reader.BaseStream.Length = int64(TestBlobContent.Length) &&
                                                                           reader.ReadBytes(TestBlobContent.Length) = TestBlobContent                                                     
                         | _ -> false)

[<Test; Category("CI")>]
let ``Array of arrays roundtrips to JSON``() =

    let ia = [| 0..50 |]
    let id = [| 1e-12; 1e+20; 3.1415; 2.87 |]
    let is = [| "Hello"; "World" |]
    let ib = [| true; false |]
    let idt = [| System.DateTime(2014,1,1); System.DateTime(2015,10,11) |]

    let arrays = InfoSet.Seq([ InfoSet.IntArray(ia); 
                               InfoSet.BoolArray(ib); 
                               InfoSet.StringArray(is); 
                               InfoSet.DoubleArray(id); 
                               InfoSet.DateTimeArray(idt) ] |> Seq.ofList)
     
    let json = Json.Marshal(arrays, None)
    System.Diagnostics.Trace.WriteLine("Array of arrays in JSON format")
    System.Diagnostics.Trace.WriteLine(json.ToString())
    let si2 = Json.Unmarshal(json, None)

    match si2 with
    | Seq(s) ->
        let a = s |> Array.ofSeq
        match a.[0] with
        | InfoSet.IntArray(r) -> r |> Array.ofSeq =! ia    
        | _ -> Assert.Fail("IntArray expected")
        match a.[1] with
        | InfoSet.BoolArray(r) -> r |> Array.ofSeq =! ib    
        | _ -> Assert.Fail("BoolArray expected")
        match a.[2] with
        | InfoSet.StringArray(r) -> r |> Array.ofSeq =! is
        | _ -> Assert.Fail("StringArray expected")
        match a.[3] with
        | InfoSet.DoubleArray(r) -> r |> Array.ofSeq =! id    
        | _ -> Assert.Fail("DoubleArray expected")
        match a.[4] with
        | InfoSet.DateTimeArray(r) -> r |> Array.ofSeq =! idt    
        | _ -> Assert.Fail("DateTimeArray expected")
    | _ -> Assert.Fail "InfoSet.Seq expected"