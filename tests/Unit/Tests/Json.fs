namespace Bolero.Tests

open System
open System.Collections.Generic
open NUnit.Framework
open FsCheck.NUnit
open FsCheck
module J = Bolero.Json

[<Category "JSON">]
module Json =

    let stringLow x = (string x).ToLowerInvariant()

    [<Property>]
    let ``Serialize bool`` (x: bool) =
        J.Serialize x .=. stringLow x

    [<Property>]
    let ``Deserialize bool`` (x: bool) =
        J.Deserialize (stringLow x) .=. x

    [<Property>]
    let ``Serialize int8`` (x: int8) =
        J.Serialize x .=. string x

    [<Property>]
    let ``Deserialize int8`` (x: int8) =
        J.Deserialize (string x) .=. x

    [<Property>]
    let ``Serialize uint8`` (x: uint8) =
        J.Serialize x .=. string x

    [<Property>]
    let ``Deserialize uint8`` (x: uint8) =
        J.Deserialize (string x) .=. x

    [<Property>]
    let ``Serialize int16`` (x: int16) =
        J.Serialize x .=. string x

    [<Property>]
    let ``Deserialize int16`` (x: int16) =
        J.Deserialize (string x) .=. x

    [<Property>]
    let ``Serialize uint16`` (x: uint16) =
        J.Serialize x .=. string x

    [<Property>]
    let ``Deserialize uint16`` (x: uint16) =
        J.Deserialize (string x) .=. x

    [<Property>]
    let ``Serialize int32`` (x: int32) =
        J.Serialize x .=. string x

    [<Property>]
    let ``Deserialize int32`` (x: int32) =
        J.Deserialize (string x) .=. x

    [<Property>]
    let ``Serialize uint32`` (x: uint32) =
        J.Serialize x .=. string x

    [<Property>]
    let ``Deserialize uint32`` (x: uint32) =
        J.Deserialize (string x) .=. x

    [<Property>]
    let ``Serialize int64`` (x: int64) =
        J.Serialize x .=. string x

    [<Property>]
    let ``Deserialize int64`` (x: int64) =
        J.Deserialize (string x) .=. x

    [<Property>]
    let ``Serialize uint64`` (x: uint64) =
        J.Serialize x .=. string x

    [<Property>]
    let ``Deserialize uint64`` (x: uint64) =
        J.Deserialize (string x) .=. x

    [<Property>]
    let ``Serialize float`` (NormalFloat x) =
        J.Serialize x .=. string x

    [<Property>]
    let ``Deserialize float`` (NormalFloat x) =
        J.Deserialize (string x) .=~. x

    [<Property>]
    let ``Serialize pair`` (x: int, y: string as t) =
        J.Encode t .=. J.Array [|J.Encode x; J.Encode y|]

    [<Property>]
    let ``Deserialize pair`` (x: int, y: string as t) =
        J.Decode (J.Array [|J.Encode x; J.Encode y|]) .=. t

    [<Property>]
    let ``Serialize triple`` (x: int, y: string, z: bool as t) =
        J.Encode t .=. J.Array [|J.Encode x; J.Encode y; J.Encode z|]

    [<Property>]
    let ``Deserialize triple`` (x: int, y: string, z: bool as t) =
        J.Decode (J.Array [|J.Encode x; J.Encode y; J.Encode z|]) .=. t

    [<Property>]
    let ``Serialize list`` (l: list<int>) =
        J.Encode l .=. J.Array [| for i in l -> J.Encode i |]

    [<Property>]
    let ``Deserialize list`` (l: list<int>) =
        J.Decode (J.Array [| for i in l -> J.Encode i |]) .=. l

    [<Property>]
    let ``Serialize array`` (l: int[]) =
        J.Encode l .=. J.Array [| for i in l -> J.Encode i |]

    [<Property>]
    let ``Deserialize array`` (l: int[]) =
        J.Decode (J.Array [| for i in l -> J.Encode i |]) .=. l

    [<Property>]
    let ``Serialize queue`` (l: int[]) =
        let q = Queue(l)
        J.Encode l .=. J.Array [| for i in q -> J.Encode i |]

    [<Property>]
    let ``Deserialize queue`` (l: int[]) =
        let q = Queue(l)
        J.Decode (J.Array [| for i in q -> J.Encode i |]) .=. l

    [<Property>]
    let ``Serialize stack`` (l: int[]) =
        let s = Stack(l)
        J.Encode l .=. J.Array (Array.rev [| for i in s -> J.Encode i |])

    [<Property>]
    let ``Deserialize stack`` (l: int[]) =
        let s = Stack(l)
        J.Decode (J.Array (Array.rev [| for i in s -> J.Encode i |])) .=. l

    [<Property>]
    let ``Serialize set`` (l: Set<int>) =
        J.Encode l .=. J.Array [| for i in l -> J.Encode i |]

    [<Property>]
    let ``Deserialize set`` (l: Set<int>) =
        J.Decode (J.Array [| for i in l -> J.Encode i |]) .=. l

    [<Property>]
    let ``Serialize string map`` (l: Map<string, int>) =
        J.Encode l .=. J.Object [| for KeyValue(k, v) in l -> k, J.Encode v |]

    [<Property>]
    let ``Deserialize string map`` (l: Map<string, int>) =
        J.Decode (J.Object [| for KeyValue(k, v) in l -> k, J.Encode v |]) .=. l

    [<Property>]
    let ``Serialize non-string map`` (l: Map<int, int>) =
        J.Encode l .=. J.Array [| for KeyValue(k, v) in l -> J.Array [| J.Encode k; J.Encode v |] |]

    [<Property>]
    let ``Deserialize non-string map`` (l: Map<int, int>) =
        J.Decode (J.Array [| for KeyValue(k, v) in l -> J.Array [| J.Encode k; J.Encode v |] |]) .=. l

    [<Property>]
    let ``Serialize string dictionary`` (l: Dictionary<string, int>) =
        J.Encode l .=. J.Object [| for KeyValue(k, v) in l -> k, J.Encode v |]

    [<Property>]
    let ``Deserialize string dictionary`` (l: Dictionary<string, int>) =
        J.Decode<Dictionary<string, int>>
            (J.Object [| for KeyValue(k, v) in l -> k, J.Encode v |])
        |> Seq.forall (fun (KeyValue(k, v)) -> l.[k] = v)

    [<Property>]
    let ``Serialize non-string dictionary`` (l: Dictionary<int, int>) =
        J.Encode l .=. J.Array [| for KeyValue(k, v) in l -> J.Array [| J.Encode k; J.Encode v |] |]

    [<Property>]
    let ``Deserialize non-string dictionary`` (l: Dictionary<int, int>) =
        J.Decode<Dictionary<int, int>>
            (J.Array [| for KeyValue(k, v) in l -> J.Array [| J.Encode k; J.Encode v |] |])
        |> Seq.forall (fun (KeyValue(k, v)) -> l.[k] = v)

    [<Property>]
    let ``Serialize UTC DateTime`` (d: DateTime) =
        let d = d.ToUniversalTime()
        J.Encode d .=. J.String (d.ToString("o"))

    [<Property>]
    let ``Deserialize UTC DateTime`` (d: DateTime) =
        let d = d.ToUniversalTime()
        J.Decode (J.String (d.ToString("o"))) .=. d

    [<Property>]
    let ``Serialize DateTimeOffset`` (d: DateTimeOffset) =
        J.Encode d .=. J.String (d.ToString("o"))

    [<Property>]
    let ``Deserialize DateTimeOffset`` (d: DateTimeOffset) =
        J.Decode (J.String (d.ToString("o"))) .=. d

    type SimpleRecord =
        {
            FieldInt : int
            FieldBool : bool
        }

        static member Enc r =
            J.Object [|"FieldInt", J.Encode r.FieldInt; "FieldBool", J.Encode r.FieldBool|]

    [<Property>]
    let ``Serialize simple record`` (r: SimpleRecord) =
        J.Encode r .=. SimpleRecord.Enc r

    [<Property>]
    let ``Deserialize simple record`` (r: SimpleRecord) =
        J.Decode (SimpleRecord.Enc r) .=. r

    type RecordWithOption =
        {
            FieldInt2 : int
            FieldOption : option<SimpleRecord>
        }

        static member Enc r =
            J.Object [|
                yield "FieldInt2", J.Encode r.FieldInt2
                match r.FieldOption with
                | None -> ()
                | Some o -> yield "FieldOption", J.Encode o
            |]

    [<Property>]
    let ``Serialize record with option field`` (r: RecordWithOption) =
        J.Encode r .=. RecordWithOption.Enc r

    [<Property>]
    let ``Deserialize record with option field`` (r: RecordWithOption) =
        J.Decode (RecordWithOption.Enc r) .=. r

    type RecordWithNamedFields =
        {
            [<J.Name "NotFieldInt3">]
            FieldInt3: int
            FieldBool3: bool
            [<J.Name "NotFieldOption3">]
            FieldOption3: option<int>
        }

        static member Enc r =
            J.Object [|
                yield "NotFieldInt3", J.Encode r.FieldInt3
                yield "FieldBool3", J.Encode r.FieldBool3
                match r.FieldOption3 with
                | None -> ()
                | Some o -> yield "NotFieldOption3", J.Encode o
            |]

    [<Property>]
    let ``Serialize record with named fields`` (r: RecordWithNamedFields) =
        J.Encode r .=. RecordWithNamedFields.Enc r

    [<Property>]
    let ``Deserialize record with named fields`` (r: RecordWithNamedFields) =
        J.Decode (RecordWithNamedFields.Enc r) .=. r

    type SimpleUnion =
        | Nullary
        | Something of x: int
        | Recursive of y: string * r: SimpleUnion
        | ImmediateSimpleRecord of SimpleRecord
        | ImmediateRecordWithOption of RecordWithOption
        | ImmediateRecordWithNamedFields of RecordWithNamedFields

        static member Enc u =
            match u with
            | Nullary -> J.Object [|"$", J.Number "0"|]
            | Something i -> J.Object [|"$", J.Number "1"; "x", J.Encode i|]
            | Recursive(s, u) -> J.Object [|"$", J.Number "2"; "y", J.Encode s; "r", SimpleUnion.Enc u|]
            | ImmediateSimpleRecord r ->
                match SimpleRecord.Enc r with
                | J.Object o -> J.Object (Array.append [|"$", J.Number "3"|] o)
                | _ -> failwith "Incorrect SimpleRecord.Enc"
            | ImmediateRecordWithOption r ->
                match RecordWithOption.Enc r with
                | J.Object o -> J.Object (Array.append [|"$", J.Number "4"|] o)
                | _ -> failwith "Incorrect RecordWithOption.Enc"
            | ImmediateRecordWithNamedFields r ->
                match RecordWithNamedFields.Enc r with
                | J.Object o -> J.Object (Array.append [|"$", J.Number "5"|] o)
                | _ -> failwith "Incorrect RecordWithNamedFields.Enc"

    [<Property>]
    let ``Serialize simple union`` (u: SimpleUnion) =
        J.Encode u .=. SimpleUnion.Enc u

    [<Property>]
    let ``Deserialize simple union`` (u: SimpleUnion) =
        J.Decode (SimpleUnion.Enc u) .=. u

    [<J.NamedUnionCases>]
    type ImplicitDiscrUnion =
        | Case1 of c1: int
        | Case2 of o: option<string> * c2: string
        | Case3 of o: option<string> * c3: int

        static member Enc u =
            match u with
            | Case1(c1=c1) -> [|"c1", J.Encode c1|]
            | Case2(o=None; c2=c2) -> [|"c2", J.Encode c2|]
            | Case2(o=Some o; c2=c2) -> [|"o", J.Encode o; "c2", J.Encode c2|]
            | Case3(o=None; c3=c3) -> [|"c3", J.Encode c3|]
            | Case3(o=Some o; c3=c3) -> [|"o", J.Encode o; "c3", J.Encode c3|]
            |> J.Object

    [<Property>]
    let ``Serialize union with parameterless NamedUnionCases`` (u: ImplicitDiscrUnion) =
        J.Encode u .=. ImplicitDiscrUnion.Enc u

    [<Property>]
    let ``Deserialize union with parameterless NamedUnionCases`` (u: ImplicitDiscrUnion) =
        J.Decode (ImplicitDiscrUnion.Enc u) .=. u

    [<J.NamedUnionCases "d">]
    type ExplicitDiscrUnion =
        | Case1 of x: int
        | [<J.Name "NotCase2">] Case2 of x: string

        static member Enc u =
            match u with
            | Case1(x=x) -> [|"d", J.String "Case1"; "x", J.Encode x|]
            | Case2(x=x) -> [|"d", J.String "NotCase2"; "x", J.Encode x|]
            |> J.Object

    [<Property>]
    let ``Serialize union with NamedUnionCases`` (u: ExplicitDiscrUnion) =
        J.Encode u .=. ExplicitDiscrUnion.Enc u

    [<Property>]
    let ``Deserialize union with NamedUnionCases`` (u: ExplicitDiscrUnion) =
        J.Decode (ExplicitDiscrUnion.Enc u) .=. u
