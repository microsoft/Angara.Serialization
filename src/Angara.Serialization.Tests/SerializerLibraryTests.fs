module SerializerLibraryTests

open Angara.Serialization
open NUnit.Framework

open Common

let Register2(s : SerializerLibrary seq) =
    s |> Seq.iter(fun lib -> lib.Register(Vector2dSerializer()))

let Register23(s : SerializerLibrary seq) =
    s |> Seq.iter(fun lib -> lib.Register(Vector2dSerializer())
                             lib.Register(Vector3dSerializer()))

[<Test; Category("CI")>]
let ``Library records registrar functions`` () =
    let lib = SerializerLibrary()
    Register2 [ lib ] // Adds registrar information
    lib.Register(Vector3dSerializer()) // No registrar information is found
    let regs = lib.GetRegistrars()
    Assert.AreEqual(1, regs.Length)
    Assert.AreEqual("Register2", regs.[0].Name)

[<Test; Category("CI")>]
let ``Library returns  array of unique registars`` () =
    let lib = SerializerLibrary()
    Register23 [ lib ] // Adds registrar information
    let regs = lib.GetRegistrars()
    Assert.AreEqual(1, regs.Length)
    Assert.AreEqual("Register23", regs.[0].Name)
