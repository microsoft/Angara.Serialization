module SerializerLibraryTests

open Angara.Serialization
open NUnit.Framework

open Common

let Register2(s : ISerializerLibrary seq) =
    s |> Seq.iter(fun lib -> lib.Register("v2d", SerializeVector2d, DeserializeVector2d))

let Register23(s : ISerializerLibrary seq) =
    s |> Seq.iter(fun lib -> lib.Register("v2d", SerializeVector2d, DeserializeVector2d)
                             lib.Register("v3d", SerializeVector3d, DeserializeVector3d))

[<Test; Category("CI")>]
let ``Library records registrar functions`` () =
    let lib = SerializerLibrary.CreateEmpty()
    Register2 [ lib ] // Adds registrar information
    let regs = lib.GetRegistrars()
    Assert.AreEqual(1, regs.Length)
    Assert.AreEqual("Register2", regs.[0].Name)

[<Test; Category("CI")>]
let ``Library returns  array of unique registars`` () =
    let lib = SerializerLibrary.CreateEmpty()
    Register23 [ lib ] // Adds registrar information
    let regs = lib.GetRegistrars()
    Assert.AreEqual(1, regs.Length)
    Assert.AreEqual("Register23", regs.[0].Name)

let genericArtefactSerializers<'x,'y> = SerializeGenericArtefact<'x,'y>, DeserializeGenericArtefact<'x,'y>

type GenericSerializer<'a,'b> () =
    interface end

[<Test; Category("CI")>]
let ``Library resolves generic TypeIds`` () =
    let lib = SerializerLibrary.CreateDefault()
    lib.RegisterGeneric("GenericArtefact", typedefof<GenericSerializer>)
    match lib.TryResolveTypeId lib "GenericArtefact<int,string>" with
    | Serializable(_) -> ((*OK*)) 
    | Transient(_) -> Assert.Fail("GenericArtefact is not transient")
    | NotFound(typeId) -> Assert.Fail(typeId + " is not resolved")

[<Test; Category("CI")>]
let ``Library resolves generic types`` () =
    let lib = SerializerLibrary.CreateDefault()
    lib.RegisterGeneric("GenericArtefact", <@@ genericArtefactSerializers @@>)        
    match lib.TryResolveType lib typeof<GenericArtefact<int,string>> with
    | Serializable(so) when so.TypeId = "GenericArtefact<int,string>"-> ((*OK*)) 
    | Serializable(_) -> Assert.Fail("Serializer with wrong TypeId is returned ")
    | Transient(_) -> Assert.Fail("GenericArtefact is not transient")
    | NotFound(typeId) -> Assert.Fail(typeId + " is not resolved")