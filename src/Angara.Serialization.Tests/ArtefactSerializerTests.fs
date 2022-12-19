module ArtefactSerializerTests
open System
open NUnit.Framework
open FsUnit
open FsCheck.NUnit

open Common
open Angara.Serialization

let checkRT (lib : ISerializerResolver) a =
    a |> ArtefactSerializer.Serialize lib |> ArtefactSerializer.Deserialize lib |> should equal a

let checkCoreRT a = a |> checkRT CoreSerializerResolver.Instance

let shouldSatisfy (lib : ISerializerResolver) (p: obj -> bool) a =
    let a' = a |> ArtefactSerializer.Serialize lib 
               |> ArtefactSerializer.Deserialize lib 
    Assert.IsTrue(p a') 

let equald d1 d2 = d1 = d2 || Double.IsNaN(d1) && Double.IsNaN(d2)

[<Property; Category("CI")>]
let ``Int64 is serialized`` (i : int64) = checkCoreRT i

[<Property; Category("CI")>]
let ``UInt64 is serialized`` (i : uint64) = checkCoreRT i

[<Property; Category("CI")>]
let ``Int32 is serialized`` (i : int) = checkCoreRT i

[<Property; Category("CI")>]
let ``UInt32 is serialized`` (i : uint32) = checkCoreRT i

[<Property; Category("CI")>]
let ``Int16 is serialized`` (i : int16) = checkCoreRT i

[<Property; Category("CI")>]
let ``UInt16 is serialized`` (i : uint16) = checkCoreRT i

[<Property; Category("CI")>]
let ``Decimal is serialized`` (i : decimal) = checkCoreRT i

[<Property; Category("CI")>]
let ``Byte is serialized`` (i : byte) = checkCoreRT i

[<Property; Category("CI")>]
let ``Bool is serialized`` (i : bool) = checkCoreRT i

[<Property; Category("CI")>]
let ``String is serialized`` (s : string) = checkCoreRT s

[<Property; Category("CI")>]
let ``DateTime is serialized`` (d : DateTime) = checkCoreRT d

[<Property; Category("CI")>]
let ``Guid is serialized`` (g : System.Guid) = checkCoreRT g

[<Property; Category("CI")>]
let ``Double is serialized`` (d : double) = 
    let lib = CoreSerializerResolver.Instance
    let d' = d |> ArtefactSerializer.Serialize lib |> ArtefactSerializer.Deserialize lib :?> double
    equald d d' |> Assert.IsTrue

[<Test; Category("CI")>]
let ``Double is serialized with full precision``() =
    checkCoreRT 0.84551240822557006

[<Test; Category("CI")>]
let ``Null is serialized``() =
    checkCoreRT null

let buildCustomLib () = 
    let lib = SerializerLibrary.CreateEmpty()
    lib.Register(Vector2dSerializer())
    SerializerCompositeResolver([ lib; CoreSerializerResolver.Instance ])

[<Property; Category("CI")>]
let ``Custom artefacts are serialized`` (v : Vector2d) = checkRT (buildCustomLib()) v

[<Test; Category("CI")>]
let ``Tuples of any length are serialized`` () =
    let lib = buildCustomLib()
    let v : Vector2d = { x = 5; y = -1 }
    checkRT lib (v, 10)
    checkRT lib ("Hello", v, 10)
    checkRT lib ("Hello", v, 10, true)
    checkRT lib ("Hello", v, 10, true, 3.1415)
    checkRT lib ("Hello", v, 10, true, 3.1415, 2.87)
    checkRT lib ("Hello", v, 10, true, 3.1415, 2.87, "foobar")
    // Tuples of 8 and more items use hierarchical representation
    checkRT lib ("Hello", v, 10, true, 3.1415, 2.87, "foobar", false) 
    checkRT lib ("Hello", v, 10, true, 3.1415, 2.87, "foobar", false, 101)

// Arrays

[<Property; Category("CI")>]
let ``Int64[] is serialized`` (i : int64 array) = checkCoreRT i

[<Property; Category("CI")>]
let ``UInt64[] is serialized`` (i : uint64 array) = checkCoreRT i

[<Property; Category("CI")>]
let ``Int[] is serialized`` (i : int array) = checkCoreRT i

[<Property; Category("CI")>]
let ``UInt[] is serialized`` (i : uint32 array) = checkCoreRT i

[<Property; Category("CI")>]
let ``Int16[] is serialized`` (i : int16 array) = checkCoreRT i

[<Property; Category("CI")>]
let ``UInt16[] is serialized`` (i : uint16 array) = checkCoreRT i

[<Property; Category("CI")>]
let ``byte[] is serialized`` (i : byte array) = checkCoreRT i

[<Property; Category("CI")>]
let ``decimal[] is serialized`` (i : decimal array) = checkCoreRT i

[<Property; Category("CI")>]
let ``string[] is serialized`` (s : string array) = checkCoreRT s

[<Property; Category("CI")>]
let ``Arrays of custom artefacts is serialized`` (i : Vector2d array) = checkRT (buildCustomLib()) i

[<Property; Category("CI")>]
let ``bool[] is serialized`` (i : bool array) = checkCoreRT i

[<Property; Category("CI")>]
let ``Array of string arrays is serialized`` (a : string array array) = checkCoreRT a

[<Property; Category("CI")>]
let ``Array of int arrays is serialized`` (a : int array array) = checkCoreRT a

[<Property; Category("CI")>]
let ``Array of string lists is serialized`` (a : string list array) = checkCoreRT a

[<Property; Category("CI")>]
let ``Array of int lists is serialized`` (a : int list array) = checkCoreRT a

// Lists

[<Property; Category("CI")>]
let ``Int64 list is serialized`` (i : int64 list) = checkCoreRT i

[<Property; Category("CI")>]
let ``UInt64 list is serialized`` (i : uint64 list) = checkCoreRT i

[<Property; Category("CI")>]
let ``Int list is serialized`` (i : int list) = checkCoreRT i

[<Property; Category("CI")>]
let ``UInt list is serialized`` (i : uint32 list) = checkCoreRT i

[<Property; Category("CI")>]
let ``Int16 list is serialized`` (i : int16 list) = checkCoreRT i

[<Property; Category("CI")>]
let ``UInt16 list is serialized`` (i : uint16 list) = checkCoreRT i

[<Property; Category("CI")>]
let ``byte list is serialized`` (i : byte list) = checkCoreRT i

[<Property; Category("CI")>]
let ``decimal list is serialized`` (i : decimal list) = checkCoreRT i

[<Property; Category("CI")>]
let ``string list is serialized`` (s : string list) = checkCoreRT s

[<Property; Category("CI")>]
let ``List of custom artefacts is serialized`` (i : Vector2d array) = checkRT (buildCustomLib()) i

[<Property; Category("CI")>]
let ``bool list is serialized`` (i : bool list) = checkCoreRT i

//[<Test; Category("CI")>]
//let ``Option artefacts are serialized and deserialized`` () =
//    let sl = SerializerResolver()
//    sl.Register(VectorSerializer())
//    Some(5) |> ArtefactSerializer.serialize sl.Library |> Option.get  
//            |> ArtefactSerializer.deserialize sl.Library |> Option.get
//            |> should equal (Some(5))
//    Some({ x = -2.5; y = 1.0 }) |> ArtefactSerializer.serialize sl.Library |> Option.get  
//                                |> ArtefactSerializer.deserialize sl.Library |> Option.get
//                                |> should equal (Some({ x = -2.5; y = 1.0 }))
//
//[<Test; Category("CI")>]
//let ``Tuple of arrays is serialized and deserialized`` () =
//    let sl = SerializerResolver()
//    sl.Register(VectorSerializer())
//    let v1 = { x = -2.5; y = 1.0 }
//    let v2 = { x = 1.0; y = -1.0 }
//    let dt1 = System.DateTime.Now
//    let dt2 = System.DateTime.Now.AddMinutes 1.0
//    let i = [| 1; 2; 3 |]
//    let f = [| 10.0; 20.0; 30.0; |]
//    let b = [| true; false |]
//    let s = [| "Hello"; "World!" |]
//    let d = [| System.DateTime.Now; System.DateTime.Now.AddMinutes 1.0 |]
//    let v = [| { x = -2.5; y = 1.0 }; { x = 1.0; y = -1.0 } |]
//    let i',f',b',s',d',v' =  (i, f, b, s, d, v)
//                             |> ArtefactSerializer.serialize (sl.Library) |> Option.get
//                             |> ArtefactSerializer.deserialize (sl.Library) |> Option.get 
//                             :?> int[] * double[] * bool[] * string[] * System.DateTime[] * Vector2d[]
//    i |> should equal i'
//    f |> should equal f'
//    b |> should equal b'
//    s |> should equal s'
//    d |> should equal d'
//    v |> should equal v'
//   
//[<Test; Category("CI")>]
//let ``List of lists serialization and deserialization`` () =
//    let sl = SerializerLibrary.Empty
//
//    let lists = [ [ 1; 2; ]; [ 10; 15; -3  ] ]
//    lists
//    |> ArtefactSerializer.serialize sl |> Option.get
//    |> ArtefactSerializer.deserialize sl |> Option.get
//    |> should equal lists
//
//    let dlists = [ [ 1.0; 2.0; ]; [ 10.0; 15.0; -3.0  ] ]
//    dlists
//    |> ArtefactSerializer.serialize sl |> Option.get
//    |> ArtefactSerializer.deserialize sl |> Option.get
//    |> should equal dlists
//
////[<Test; Category("CI")>]
////let ``Map<string, obj> serialization and deserialization``() =
////    let sl = SerializerLibrary()
////    let jtb = JToken.Parse(@"{
////        ""i:int"": 1,
////        ""d"": 1.0,
////        ""s"": ""value"",
////        ""b"": true,
////        ""iarr:int[]"": [ 1, 2, 3 ],
////        ""darr:double[]"": [ 1.0, 2.0, 3.0 ],
////        ""barr:bool[]"": [ true, false, true ],
////        ""sarr:string[]"": [ ""1"", ""2"", ""3"" ],
////        ""earr:object[]"": [],
////        ""elist"": { ""$typeID"": ""list"", ""$value:object[]"": [] },
////        ""map"": { ""i:int"": 1, ""s"": ""value"" },
////        ""marr:object[]"": [ { ""i:int"": 1, ""s"": ""value"" } ]
////    }")
////
////    // Update.
////    let isb = Angara.Persistence.Utils.GetInfoSet jtb
////    let mb = isb |> ArtefactSerializer.deserialize sl |> Option.get
////
////    // Get.
////    let isa = mb |> ArtefactSerializer.serialize sl |> Option.get
////    let jta = Angara.Persistence.Utils.GetJson isa
////
////    for prop in (jtb :?> JObject).Properties() do
////        let key = prop.Name
////        if not (key.Contains "[]") then jta.[key].ToString() =? jtb.[key].ToString()
////
////    System.Convert.FromBase64String (jta.["iarr:int[]"].ToString()) =? (jtb.["iarr:int[]"].ToObject<array<int>>() |> Array.map (fun v -> System.BitConverter.GetBytes v) |> Array.concat)
////    System.Convert.FromBase64String (jta.["darr:double[]"].ToString()) =? (jtb.["darr:double[]"].ToObject<array<double>>() |> Array.map (fun v -> System.BitConverter.GetBytes v) |> Array.concat)
////    System.Convert.FromBase64String (jta.["barr:bool[]"].ToString()) =? (jtb.["barr:bool[]"].ToObject<array<bool>>() |> Array.map (fun v -> System.BitConverter.GetBytes v) |> Array.concat)
////    jta.["marr:object[]"].ToString() =? jtb.["marr:object[]"].ToString()
////    jta.["sarr:string[]"].ToString() =? jtb.["sarr:string[]"].ToString()
//
//[<Test; Category("CI")>]
//let ``Empty array and empty list as values in Map<string, obj> serialization and deserialization``() =
//    let sl = SerializerLibrary.Empty
//    let m = Map.empty
//               .Add("empty array", [||] :> obj) 
//               .Add("empty list", List.empty<Map<string,obj>> :> obj) // We cannot serialize lists of objs, just lists of Map<string,obj>
//
//    let sm = m |> ArtefactSerializer.serialize sl |> Option.get
//    let dm = sm |> ArtefactSerializer.deserialize sl |> Option.get :?> Map<string, obj>
//    dm =? m
//
//[<Test; Category("CI")>]
//let ``Infoset map with seq without type info deserialized to array``() =
//    let sl = SerializerLibrary.Empty
//    let ints = seq [ InfoSet.Int 1; InfoSet.Int 2; ]
//    let emptys = seq []
//
//    let sm = InfoSet.EmptyMap
//                    .AddInfoSet("int seq", InfoSet.Seq ints)
//                    .AddInfoSet("empty seq", InfoSet.Seq emptys)
//
//    let m = Map.empty
//               .Add("int seq", [| 1; 2; |] :> obj)
//               .Add("empty seq", [||] :> obj)
//
//    let dm = sm |> ArtefactSerializer.deserialize sl |> Option.get :?> Map<string, obj>
//    dm =? m
//
////[<Test; Category("CI")>]
////let ``Map<string, obj> with type info serialization and deserialization``() =
////    let sl = SerializerLibrary()
////    let jtb = JToken.Parse(@"{
////        ""earr"": { ""$typeID"": ""Vector2d v1 array"", ""$value:object[]"": [] },
////        ""elist"": { ""$typeID"": ""Vector2d v1 list"", ""$value:object[]"": [] },
////        ""arr"": { ""$typeID"": ""Vector2d v1 array"", ""$value:object[]"": [ { ""x"": 1.0, ""y"": 1.0 } ] },
////        ""list"": { ""$typeID"": ""Vector2d v1 list"", ""$value:object[]"": [ { ""x"": 1.0, ""y"": 1.0 } ] },
////        ""map"": { ""$typeID"": ""Vector2d v1"", ""x"": 1.0, ""y"": 1.0 }
////    }")
////
////    sl.Register<Vector2d> serializeVector deserializeVector "Vector2d"
////
////    // Update.
////    let isb = Angara.Persistence.Utils.GetInfoSet jtb
////    let mb = isb |> ArtefactSerializer.deserialize sl |> Option.get
////
////    // Get.
////    let isa = mb |> ArtefactSerializer.serialize sl |> Option.get
////    let jta = Angara.Persistence.Utils.GetJson isa
////
////    for prop in (jtb :?> JObject).Properties() do
////        let key = prop.Name
////        jta.[key].ToString() =? jtb.[key].ToString()
//
