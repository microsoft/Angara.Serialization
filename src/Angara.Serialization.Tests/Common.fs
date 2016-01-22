module Common

open Angara.Serialization

type Vector2d = 
    { x: int; y: int }

type Vector3d = 
    { x: int; y: int; z: int }

type Vector2dSerializer() =
    interface ISerializer<Vector2d> with
        member x.TypeId = "Vector2d"
        member x.Serialize _ v = InfoSet.EmptyMap.AddInt("x", v.x).AddInt("y", v.y)
        member x.Deserialize _ si = let map = InfoSet.toMap si in { x = map.["x"].ToInt(); y = map.["y"].ToInt() } 

type Vector3dSerializer() =
    interface ISerializer<Vector3d> with
        member x.TypeId = "Vector3d"
        member x.Serialize _ v = InfoSet.EmptyMap.AddInt("x", v.x).AddInt("y", v.y).AddInt("z", v.z)
        member x.Deserialize _ si = let map = InfoSet.toMap si in { x = map.["x"].ToInt(); y = map.["y"].ToInt(); z = map.["z"].ToInt() } 

