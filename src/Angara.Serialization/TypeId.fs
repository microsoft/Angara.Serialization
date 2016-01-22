namespace Angara.Serialization

module TypeIds =
    module Suffixes = 
        [<Literal>] 
        let Array = "array"
        [<Literal>] 
        let Option = "option"
        [<Literal>] 
        let List = "list"

    // Built-in TypeIds 
    [<Literal>] 
    let Array = "array"
    [<Literal>]
    let Object = "object"
    [<Literal>] 
    let Option = "option"
    [<Literal>] 
    let List = "list"
    [<Literal>] 
    let Tuple = "tuple"
    [<Literal>]
    let Int = "int"
    [<Literal>]
    let UInt = "uint"
    [<Literal>]
    let Int64 = "int64"
    [<Literal>]
    let UInt64 = "uint64"
    [<Literal>]
    let Int16 = "int16"
    [<Literal>]
    let UInt16 = "uint16"
    [<Literal>]
    let Byte = "byte"
    [<Literal>]
    let Decimal = "decimal"
    [<Literal>]
    let Double = "double"
    [<Literal>]
    let Bool = "bool"
    [<Literal>]
    let DateTime = "datetime"
    [<Literal>]
    let String = "string"
    [<Literal>]
    let Guid = "guid"
      
type TypeId = 
    | Simple of string
    | Generic of string * TypeId list
    
    override x.ToString() = 
        match x with
        | Simple(t) -> t
        | Generic(t,a) ->
            if (t = TypeIds.List || t = TypeIds.Array || t = TypeIds.Option) && a.Length = 1
            then a.Head.ToString() + " " + t
            else TypeId.ToString(t, a |> List.map (fun a -> a.ToString()))
    
    static member ToString (typeId : string, argIds : string seq) =  typeId + "<" + (argIds |> String.concat ",") + ">" 

    static member TryParse (s:string) = 

        let rec split (s : string, p : int, nested : int, parts : string list) = 
            if p < 0 then s :: parts
            else 
                match s.[p] with
                | '>' -> split(s, p - 1, nested + 1, parts)
                | '<' -> split(s, p - 1, nested - 1, parts)
                | ',' -> if nested > 0 
                         then split(s, p - 1, nested, parts)
                         else split(s.Substring(0, p), p - 1, nested, s.Substring(p+1) :: parts)                             
                | _ -> split(s, p - 1, nested, parts)

        let inner, outer = 
            if s.EndsWith (" " + TypeIds.Suffixes.Array) then s.Substring(0, s.Length - TypeIds.Suffixes.Array.Length - 1), Some(TypeIds.Array)
            elif s.EndsWith (" " + TypeIds.Suffixes.Option) then s.Substring(0, s.Length - TypeIds.Suffixes.Option.Length - 1), Some(TypeIds.Option)
            elif s.EndsWith (" " + TypeIds.Suffixes.List) then s.Substring(0, s.Length - TypeIds.Suffixes.List.Length - 1), Some(TypeIds.List)
            else s, None // No suffix detected

        match inner, outer with 
        | inner, Some(outer) -> // 'T array', 'T list' or 'T option'
            match TypeId.TryParse(inner) with 
                  | Some(ti) -> Some(Generic(outer, [ ti ]))
                  | None -> None 
        | s, None ->
            match s.IndexOf('<') with
            | -1 -> Some(Simple(s)) 
            | 0 -> None // No type id
            | i -> match s.EndsWith(">") with
                   | false -> None // Syntax error
                   | true -> let args = s.Substring(i + 1, s.Length - i - 2)
                             let a = split(args, args.Length - 1, 0, []) |> List.map TypeId.TryParse
                             if a |> Seq.exists Option.isNone
                             then None
                             else Some(Generic(s.Substring(0,i), a |> Seq.map Option.get |> List.ofSeq))

    static member MakeList itemTypeId = itemTypeId + " " + TypeIds.Suffixes.List

    static member MakeArray itemTypeId = itemTypeId + " " + TypeIds.Suffixes.Array

    static member MakeOption itemTypeId = itemTypeId + " " + TypeIds.Suffixes.Option