module Common

open Angara.Serialization

type Vector2d = 
    { x: int; y: int }

let SerializeVector2d (_, v : Vector2d) = InfoSet.EmptyMap.AddInt("x", v.x).AddInt("y", v.y)
let DeserializeVector2d (_, si) = let map = InfoSet.toMap si in { x = map.["x"].ToInt(); y = map.["y"].ToInt() }

type Vector3d = 
    { x: int; y: int; z: int }

let SerializeVector3d (_, v) = InfoSet.EmptyMap.AddInt("x", v.x).AddInt("y", v.y).AddInt("z", v.z)
let DeserializeVector3d (_, si) = let map = InfoSet.toMap si in { x = map.["x"].ToInt(); y = map.["y"].ToInt(); z = map.["z"].ToInt() }

type GenericArtefact<'x,'y> () = class end

let SerializeGenericArtefact<'x,'y>(_ : ISerializerResolver, artefact : GenericArtefact<'x,'y>) = InfoSet.EmptyMap
let DeserializeGenericArtefact<'x,'y>(_ : ISerializerResolver, infoSet : InfoSet) = GenericArtefact<'x,'y>()
