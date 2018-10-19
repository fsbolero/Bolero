namespace MiniBlazor.Tests

open System
open System.Collections.Generic
open NUnit.Framework
open FsCheck.NUnit
open FsCheck
module J = MiniBlazor.Json

module Json =

    let stringLow x = (string x).ToLowerInvariant()

    [<Property>]
    let ``Serialize bool`` (x: bool) =
        J.Serialize x = stringLow x

    [<Property>]
    let ``Deserialize bool`` (x: bool) =
        J.Deserialize (stringLow x) = x

    [<Property>]
    let ``Serialize int8`` (x: int8) =
        J.Serialize x = string x

    [<Property>]
    let ``Deserialize int8`` (x: int8) =
        J.Deserialize (string x) = x

    [<Property>]
    let ``Serialize uint8`` (x: uint8) =
        J.Serialize x = string x

    [<Property>]
    let ``Deserialize uint8`` (x: uint8) =
        J.Deserialize (string x) = x

    [<Property>]
    let ``Serialize int16`` (x: int16) =
        J.Serialize x = string x

    [<Property>]
    let ``Deserialize int16`` (x: int16) =
        J.Deserialize (string x) = x

    [<Property>]
    let ``Serialize uint16`` (x: uint16) =
        J.Serialize x = string x

    [<Property>]
    let ``Deserialize uint16`` (x: uint16) =
        J.Deserialize (string x) = x

    [<Property>]
    let ``Serialize int32`` (x: int32) =
        J.Serialize x = string x

    [<Property>]
    let ``Deserialize int32`` (x: int32) =
        J.Deserialize (string x) = x

    [<Property>]
    let ``Serialize uint32`` (x: uint32) =
        J.Serialize x = string x

    [<Property>]
    let ``Deserialize uint32`` (x: uint32) =
        J.Deserialize (string x) = x

    [<Property>]
    let ``Serialize int64`` (x: int64) =
        J.Serialize x = string x

    [<Property>]
    let ``Deserialize int64`` (x: int64) =
        J.Deserialize (string x) = x

    [<Property>]
    let ``Serialize uint64`` (x: uint64) =
        J.Serialize x = string x

    [<Property>]
    let ``Deserialize uint64`` (x: uint64) =
        J.Deserialize (string x) = x

    [<Property>]
    let ``Serialize float`` (NormalFloat x) =
        J.Serialize x = string x

    [<Property>]
    let ``Deserialize float`` (NormalFloat x) =
        J.Deserialize (string x) =~ x

    [<Property>]
    let ``Serialize pair`` (x: int, y: string as t) =
        J.Encode t = J.Array [|J.Encode x; J.Encode y|]

    [<Property>]
    let ``Deserialize pair`` (x: int, y: string as t) =
        J.Decode (J.Array [|J.Encode x; J.Encode y|]) = t

    [<Property>]
    let ``Serialize triple`` (x: int, y: string, z: bool as t) =
        J.Encode t = J.Array [|J.Encode x; J.Encode y; J.Encode z|]

    [<Property>]
    let ``Deserialize triple`` (x: int, y: string, z: bool as t) =
        J.Decode (J.Array [|J.Encode x; J.Encode y; J.Encode z|]) = t

    [<Property>]
    let ``Serialize list`` (l: list<int>) =
        J.Encode l = J.Array [| for i in l -> J.Encode i |]

    [<Property>]
    let ``Deserialize list`` (l: list<int>) =
        J.Decode (J.Array [| for i in l -> J.Encode i |]) = l

    [<Property>]
    let ``Serialize array`` (l: int[]) =
        J.Encode l = J.Array [| for i in l -> J.Encode i |]

    [<Property>]
    let ``Deserialize array`` (l: int[]) =
        J.Decode (J.Array [| for i in l -> J.Encode i |]) = l

    [<Property>]
    let ``Serialize queue`` (l: int[]) =
        let q = Queue(l)
        J.Encode l = J.Array [| for i in q -> J.Encode i |]

    [<Property>]
    let ``Deserialize queue`` (l: int[]) =
        let q = Queue(l)
        J.Decode (J.Array [| for i in q -> J.Encode i |]) = l

    [<Property>]
    let ``Serialize stack`` (l: int[]) =
        let s = Stack(l)
        J.Encode l = J.Array (Array.rev [| for i in s -> J.Encode i |])

    [<Property>]
    let ``Deserialize stack`` (l: int[]) =
        let s = Stack(l)
        J.Decode (J.Array (Array.rev [| for i in s -> J.Encode i |])) = l

    [<Property>]
    let ``Serialize set`` (l: Set<int>) =
        J.Encode l = J.Array [| for i in l -> J.Encode i |]

    [<Property>]
    let ``Deserialize set`` (l: Set<int>) =
        J.Decode (J.Array [| for i in l -> J.Encode i |]) = l

    [<Property>]
    let ``Serialize string map`` (l: Map<string, int>) =
        J.Encode l = J.Object [| for KeyValue(k, v) in l -> k, J.Encode v |]

    [<Property>]
    let ``Deserialize string map`` (l: Map<string, int>) =
        J.Decode (J.Object [| for KeyValue(k, v) in l -> k, J.Encode v |]) = l

    [<Property>]
    let ``Serialize non-string map`` (l: Map<int, int>) =
        J.Encode l = J.Array [| for KeyValue(k, v) in l -> J.Array [| J.Encode k; J.Encode v |] |]

    [<Property>]
    let ``Deserialize non-string map`` (l: Map<int, int>) =
        J.Decode (J.Array [| for KeyValue(k, v) in l -> J.Array [| J.Encode k; J.Encode v |] |]) = l

    [<Property>]
    let ``Serialize string dictionary`` (l: Dictionary<string, int>) =
        J.Encode l = J.Object [| for KeyValue(k, v) in l -> k, J.Encode v |]

    [<Property>]
    let ``Deserialize string dictionary`` (l: Dictionary<string, int>) =
        J.Decode<Dictionary<string, int>>
            (J.Object [| for KeyValue(k, v) in l -> k, J.Encode v |])
        |> Seq.forall (fun (KeyValue(k, v)) -> l.[k] = v)

    [<Property>]
    let ``Serialize non-string dictionary`` (l: Dictionary<int, int>) =
        J.Encode l = J.Array [| for KeyValue(k, v) in l -> J.Array [| J.Encode k; J.Encode v |] |]

    [<Property>]
    let ``Deserialize non-string dictionary`` (l: Dictionary<int, int>) =
        J.Decode<Dictionary<int, int>>
            (J.Array [| for KeyValue(k, v) in l -> J.Array [| J.Encode k; J.Encode v |] |])
        |> Seq.forall (fun (KeyValue(k, v)) -> l.[k] = v)

    [<Property>]
    let ``Serialize UTC DateTime`` (d: DateTime) =
        let d = d.ToUniversalTime()
        J.Encode d = J.String (d.ToString("o"))

    [<Property>]
    let ``Deserialize UTC DateTime`` (d: DateTime) =
        let d = d.ToUniversalTime()
        J.Decode (J.String (d.ToString("o"))) = d

    [<Property>]
    let ``Serialize DateTimeOffset`` (d: DateTimeOffset) =
        J.Encode d = J.String (d.ToString("o"))

    [<Property>]
    let ``Deserialize DateTimeOffset`` (d: DateTimeOffset) =
        J.Decode (J.String (d.ToString("o"))) = d


    /// For some reason dotnet test won't run if there are only `[<Property>]`s
    /// and not at lest one `[<Test>]`...
    [<Test>] let Dummy() = ()
