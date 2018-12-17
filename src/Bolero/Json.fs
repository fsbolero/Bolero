// $begin{copyright}
//
// This file is part of Bolero
//
// Copyright (c) 2018 IntelliFactory and contributors
//
// Licensed under the Apache License, Version 2.0 (the "License"); you
// may not use this file except in compliance with the License.  You may
// obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
// implied.  See the License for the specific language governing
// permissions and limitations under the License.
//
// $end{copyright}

module Bolero.Json

open System
open System.IO
open System.Collections.Concurrent
open System.Collections.Generic
open System.Reflection
open System.Text
open System.Text.RegularExpressions
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns

type private A = Attribute
type private T = AttributeTargets
type private U = AttributeUsageAttribute

[<Sealed; U(T.Property|||T.Field)>]
type NameAttribute =
    inherit A
    new (name: string) = { inherit A() }

[<Sealed; U(T.Property, AllowMultiple = true)>]
type DateTimeFormatAttribute =
    inherit A
    new (format: string) = { inherit A() }
    new (argumentName: string, format: string) = { inherit A() }

[<Sealed; U(T.Class)>]
type NamedUnionCasesAttribute =
    inherit A
    new (discriminatorName: string) = { inherit A() }
    new () = { inherit A() }

[<Sealed; U(T.Property)>]
type ConstantAttribute =
    inherit A
    new (value: bool) = { inherit A() }
    new (value: int) = { inherit A() }
    new (value: float) = { inherit A() }
    new (value: string) = { inherit A() }

type Value =
    | Null
    | True
    | False
    | Number of string
    | String of string
    | Array  of Value[]
    | Object of (string * Value)[]

module Raw =

    exception ReadException
    exception WriteException

    let readNumber (w: StringBuilder) (tr: TextReader) =
        let c (x: char) = w.Append x |> ignore
        let read () = tr.Read()
        let peek () = tr.Peek()
        let skip () = tr.Read() |> ignore
        let readDigits () =
            let rec loop () =
                match peek () with
                | n when n >= 48 && n <= 57 ->
                    skip (); c (char n); loop ()
                | _ -> ()
            loop ()
        match peek () with
        | 45 -> skip (); c '-'
        | _ -> ()
        match read () with
        | n when n >= 49 && n <= 57 ->
            c (char n); readDigits ()
        | 48 -> c '0'
        | _ -> raise ReadException
        match peek () with
        | 46 -> skip (); c '.'; readDigits ()
        | _ -> ()
        match peek () with
        | 101 | 69 ->
            skip (); c 'E'
            match peek () with
            | 45 -> skip (); c '-'
            | 43 -> skip (); c '+'
            | _ -> ()
            readDigits ()
        | _ ->
            ()
        let text = w.ToString()
        w.Remove(0, w.Length) |> ignore
        Number text

    let readStartedString (w: StringBuilder) (tr: TextReader) =
        let c (x: char) = w.Append x |> ignore
        let read () = tr.Read()
        let rec loop () =
            match read() with
            | 34 -> ()
            | -1 -> raise ReadException
            | 92 ->
                match read () with
                | 34 -> c '"'
                | 92 -> c '\\'
                | 47 -> c '/'
                | 98 -> c '\b'
                | 102 -> c '\f'
                | 110 -> c '\n'
                | 114 -> c '\r'
                | 116 -> c '\t'
                | 117 ->
                    let hex () =
                        match read () with
                        | n when n >= 97 && n <= 102 ->
                            n - 97 + 10
                        | n when n >= 65 && n <= 70 ->
                            n - 65 + 10
                        | n when n >= 48 && n <= 57 ->
                            n - 48
                        | _ ->
                            raise ReadException
                    let inline ( * ) a b = (a <<< 4) + b
                    c (char (hex () * hex () * hex () * hex ()))
                | _ ->
                    raise ReadException
                loop ()
            | x ->
                let x = char x
                c x
                loop ()
        loop ()
        let text = w.ToString()
        w.Remove(0, w.Length) |> ignore
        text

    let readString w (tr: TextReader) =
        match tr.Read() with
        | 34 ->
            readStartedString w tr
        | _ ->
            raise ReadException

    let readIdent (w: StringBuilder) (tr: TextReader) =
        let c (x: char) = w.Append x |> ignore
        let read () = tr.Read()
        let peek () = tr.Peek()
        let isStartChar chr =
            (65 <= chr && chr <= 90)
            || (97 <= chr && chr <= 122)
            || chr = 95
            || chr = 36
        let isContChar chr =
            isStartChar chr
            || (48 <= chr && chr <= 57)
        match read () with
        | 34 -> readStartedString w tr
        | chr when isStartChar chr ->
            c (char chr)
            while (isContChar (peek ())) do
                c (read () |> char)
            let text = w.ToString()
            w.Remove(0, w.Length) |> ignore
            text
        | _ ->
            raise ReadException

    let readSpace (tr: TextReader) =
        let rec loop () =
            match tr.Peek() with
            | n when Char.IsWhiteSpace (char n) ->
                tr.Read() |> ignore
                loop ()
            | _ ->
                ()
        loop ()

    let rec readJson (w: StringBuilder) (tr: TextReader) =
        let read () = tr.Read()
        let peek () = tr.Peek()
        let skip () = tr.Read() |> ignore
        readSpace tr
        match peek () with
        | 110 ->
            if read () = 110
                && read () = 117
                && read () = 108
                && read () = 108
            then
                Null
            else
                raise ReadException
        | 116 ->
            if read() = 116
                && read() = 114
                && read() = 117
                && read() = 101
            then
                True
            else
                raise ReadException
        | 102 ->
            if read() = 102
                && read() = 97
                && read() = 108
                && read() = 115
                && read() = 101
            then
                False
            else
                raise ReadException
        | 34 ->
            String (readString w tr)
        | 45 | 48 | 49 | 50 | 51 | 52 | 53 | 54 | 55 | 56 | 57 ->
            readNumber w tr
        | 123 ->
            skip ()
            readSpace tr
            match peek () with
            | 125 ->
                skip ()
                Object [||]
            | _ ->
                let readPair () =
                    let n = readIdent w tr
                    readSpace tr
                    if not (read() = 58) then
                        raise ReadException
                    readSpace tr
                    let j = readJson w tr
                    readSpace tr
                    (n, j)
                let p = readPair ()
                let acc = ResizeArray([|p|])
                let rec loop () =
                    match read () with
                    | 125 -> acc.ToArray()
                    | 44 ->
                        readSpace tr
                        acc.Add(readPair ())
                        loop ()
                    | _ -> raise ReadException
                Object (loop ())
        | 91 ->
            skip ()
            readSpace tr
            match peek () with
            | 93 ->
                skip ()
                Array [||]
            | _ ->
                let j = readJson w tr
                readSpace tr
                let acc = ResizeArray([|j|])
                let rec loop () =
                    readSpace tr
                    match read () with
                    | 44 ->
                        let j = readJson w tr
                        readSpace tr
                        acc.Add(j)
                        loop ()
                    | 93 -> acc.ToArray()
                    | _ -> raise ReadException
                Array (loop ())
        | _ ->
            raise ReadException

    let numberPattern =
        let pat = @"^([-]?(0|[1-9]\d*))([.]\d+)?([eE][-+]?\d+)?$"
        Regex(pat, RegexOptions.Compiled)

    let rec Write (writer: TextWriter) (value: Value) =
        let c (x: char) = writer.Write x
        let s (x: string) = writer.Write x
        let wJ x = Write writer x
        let wA x =
            match x with
            | [||] -> s "[]"
            | xs ->
                c '['
                wJ xs.[0]
                for i = 1 to xs.Length-1 do
                    c ','
                    wJ xs.[i]
                c ']'
        let wN (x: string) =
            if x <> null && numberPattern.IsMatch x then
                s x
            else
                raise WriteException
        let wS (x: string) =
            if x = null then s "null" else
            c '"'
            for i in 0 .. x.Length - 1 do
                match x.[i] with
                | '"' -> s "\\\""
                | '/' -> s "\\/"
                | '\\' -> s "\\\\"
                | '\b' -> s "\\b"
                | '\n' -> s "\\n"
                | '\r' -> s "\\r"
                | '\t' -> s "\\t"
                | '\012' -> s "\\f"
                | x ->
                    if Char.IsControl x then
                        writer.Write("\\u{0:x4}", int x)
                    else
                        c x
            c '"'
        let wO x =
            match x with
            | [||] -> s "{}"
            | xs ->
                let pair (n, x) =
                    wS n
                    c ':'
                    wJ x
                c '{'
                pair xs.[0]
                for i in 1..xs.Length-1 do
                    c ','
                    pair xs.[i]
                c '}'
        match value with
        | Null -> s "null"
        | True -> s "true"
        | False -> s "false"
        | Number x -> wN x
        | String x -> wS x
        | Array x -> wA x
        | Object x -> wO x

    let Read (tr: TextReader) : Value =
        let w = new StringBuilder()
        readJson w tr

    let Parse s =
        use r = new StringReader(s)
        Read r

    let Stringify v =
        use w = new StringWriter()
        Write w v
        w.ToString()

exception DecoderException of value:Value * typ:Type with
    override this.Message =
        "Failed to deserialize value \"" + Raw.Stringify this.value + "\" as type " + string this.typ  

exception EncoderException

exception NoDecoderException of typ:Type with
    override this.Message =
        "No JSON decoder for " + string this.typ

exception NoEncoderException of typ:Type with
    override this.Message =
        "No JSON encoder for " + string this.typ

type Decoder<'T> = Value -> 'T
type Encoder<'T> = 'T -> Value

type FST = Reflection.FSharpType
type FSV = Reflection.FSharpValue

let flags = BindingFlags.Public ||| BindingFlags.NonPublic

type TAttrs =
    {
        OptionalField : bool
        NullableUnion : bool
        DateTimeFormat : string option
        Type : Type
    }

    static member inline GetName(mi) =
        let customName =
            (^T : (member GetCustomAttributesData : unit -> IList<CustomAttributeData>) (mi))
            |> Seq.tryPick (fun cad ->
                if cad.Constructor.DeclaringType = typeof<NameAttribute> then
                    Some (cad.ConstructorArguments.[0].Value :?> string)
                else None)
        defaultArg customName (^T : (member Name : string) (mi))

    static member Get(t: Type, ?mi: #MemberInfo, ?uci: Reflection.UnionCaseInfo) =
        let mcad =
            match mi with
            | Some mi -> mi.GetCustomAttributesData()
            | None -> [||] :> _
        let ucad =
            match uci with
            | Some uci -> uci.GetCustomAttributesData()
            | None -> [||] :> _
        let isOptionalField =
            t.IsGenericType &&
            t.GetGenericTypeDefinition() = typedefof<option<_>>
        let isNullableUnion =
            FST.IsUnion(t, flags) &&
            mcad |> Seq.exists (fun cad ->
                cad.Constructor.DeclaringType = typeof<CompilationRepresentationAttribute> &&
                let flags = cad.ConstructorArguments.[0].Value :?> CompilationRepresentationFlags
                flags.HasFlag CompilationRepresentationFlags.UseNullAsTrueValue)
        let dateTimeFormat =
            mcad
            |> Seq.tryPick (fun cad ->
                if cad.Constructor.DeclaringType = typeof<DateTimeFormatAttribute> &&
                   cad.ConstructorArguments.Count = 1 then
                    Some (cad.ConstructorArguments.[0].Value :?> string)
                else None)
            |> Option.orElseWith (fun () ->
                ucad |> Seq.tryPick (fun cad ->
                    if cad.Constructor.DeclaringType = typeof<DateTimeFormatAttribute> &&
                       cad.ConstructorArguments.Count = 2 &&
                       mi.IsSome &&
                       cad.ConstructorArguments.[0].Value :?> string = mi.Value.Name then
                        Some (cad.ConstructorArguments.[1].Value :?> string)
                    else None))
        {
            OptionalField = isOptionalField
            NullableUnion = isNullableUnion
            DateTimeFormat = dateTimeFormat
            Type = t
        }

type Serializer =
    {
        Decode : Value -> obj
        Encode : obj -> Value
    }

let simple enc dec =
    {
        Encode = fun x -> enc x
        Decode = fun x -> dec x
    }

let numeric<'T> dec =
    let enc (x: obj) =
        match x with
        | null -> Null
        | :? 'T as x -> Number (string (x :> obj))
        | _ -> raise EncoderException
    let dec = function
        | Null -> box (Unchecked.defaultof<'T>)
        | Number x ->
            match dec x with
            | true, (x: 'T) -> box x
            | _ -> raise (DecoderException(Number x, typeof<'T>))
        | x -> raise (DecoderException(x, typeof<'T>))
    simple enc dec

let addNumeric<'T> (dec: string -> bool * 'T) (d: Dictionary<_,_>) =
    d.[typeof<'T>] <- numeric dec

let add<'T> (e: 'T -> Value) (d: Value -> 'T) (dict: Dictionary<_,_>) =
    let enc (x: obj) =
        match x with
        | null -> Null
        | :? 'T as x -> e x
        | _ -> raise EncoderException
    let dec = function
        | Null -> box (Unchecked.defaultof<'T>)
        | x -> box (d x)
    dict.[typeof<'T>] <- simple enc dec

let tryParseSingle x = 
    Single.TryParse(x, 
        Globalization.NumberStyles.Float, 
        Globalization.NumberFormatInfo.InvariantInfo)

let tryParseDouble x = 
    Double.TryParse(x, 
        Globalization.NumberStyles.Float, 
        Globalization.NumberFormatInfo.InvariantInfo)

let serializers =
    let d = Dictionary()
    addNumeric Byte.TryParse d
    addNumeric SByte.TryParse d
    addNumeric Int16.TryParse d
    addNumeric Int32.TryParse d
    addNumeric Int64.TryParse d
    addNumeric UInt16.TryParse d
    addNumeric UInt32.TryParse d
    addNumeric UInt64.TryParse d
    addNumeric tryParseSingle d
    addNumeric tryParseDouble d
    let encBool = function
        | true -> True
        | false -> False
    let decBool = function
        | True -> true
        | False -> false
        | x -> raise (DecoderException(x, typeof<bool>))
    add encBool decBool d
    let encChar (c: char) =
        String (string c)
    let decChar = function
        | String x ->
            match Char.TryParse x with
            | true, c -> c
            | _ -> raise (DecoderException(Number x, typeof<char>))
        | x -> raise (DecoderException(x, typeof<char>))
    add encChar decChar d
    let decString = function
        | String x -> x
        | x -> raise (DecoderException(x, typeof<string>))
    add String decString d
    let encTimeSpan (t: TimeSpan) =
        Number (string t.TotalMilliseconds)
    let decTimeSpan = function
        | Number x ->
            match tryParseDouble x with
            | true, x -> TimeSpan.FromMilliseconds x
            | _ -> raise (DecoderException(Number x, typeof<TimeSpan>))
        | x -> raise (DecoderException(x, typeof<TimeSpan>))
    add encTimeSpan decTimeSpan d
    let encGuid (g: Guid) =
        String (string g)
    let decGuid = function
        | String g -> 
            match Guid.TryParse g with
            | true, g -> g
            | _ -> raise (DecoderException(String g, typeof<Guid>))
        | x -> raise (DecoderException(x, typeof<Guid>))
    add encGuid decGuid d   
    let encDecimal (d: decimal) =
        String (string d)
    let decDecimal = function
        | String d as x ->
            match Decimal.TryParse d with
            | true, d -> d
            | _ -> raise (DecoderException(x, typeof<decimal>)) 
        | x -> raise (DecoderException(x, typeof<decimal>))
    add encDecimal decDecimal d   
    d

let tupleEncoder dE (ta: TAttrs) =
    let e = Array.map (fun t -> dE (TAttrs.Get(t))) (FST.GetTupleElements ta.Type)
    let r = FSV.PreComputeTupleReader ta.Type
    fun (x: obj) ->
        match x with
        | null ->
            raise EncoderException
        | o when o.GetType() = ta.Type ->
            Array (Array.map2 (fun e x -> e x) e (r o))
        | _ ->
            raise EncoderException

let tupleDecoder dD (ta: TAttrs) =
    let e = Array.map (fun t -> dD (TAttrs.Get(t))) (FST.GetTupleElements ta.Type)
    let c = FSV.PreComputeTupleConstructor ta.Type
    fun (x: Value) ->
        match x with
        | Array xs ->
            if xs.Length = e.Length then
                c (Array.map2 (fun e x -> e x) e xs)
            else
                raise (DecoderException(x, ta.Type))
        | _ ->
            raise (DecoderException(x, ta.Type))

let arrayEncoder dE (ta: TAttrs) =
    let e = dE (TAttrs.Get(ta.Type.GetElementType()))
    fun (x: obj) ->
        match x with
        | null ->
            Null
        | o when o.GetType() = ta.Type ->
            let o = o :?> Array
            Seq.cast o
            |> Seq.map e
            |> Seq.toArray
            |> Array
        | _ ->
            raise EncoderException

let arrayDecoder dD (ta: TAttrs) =
    let eT = ta.Type.GetElementType()
    let e = dD (TAttrs.Get(eT))
    fun (x: Value) ->
        match x with
        | Null ->
            null
        | Array xs ->
            let data = Array.map e xs
            let k = data.Length
            let r = Array.CreateInstance(eT, k)
            Array.Copy(data, r, k)
            r :> obj
        | _ ->
            raise (DecoderException(x, ta.Type))

let table ts =
    let d = Dictionary()
    for (k, v) in ts do
        d.[k] <- v
    fun x ->
        match d.TryGetValue x with
        | true, x -> Some x
        | _ -> None

/// Get the MethodInfo corresponding to a let-bound function or value,
/// parameterized with the given types.
let genLetMethod (e: Expr, ts: Type[]) =
    match e with
    | Lambda(_, Lambda(_, Call(None, m, [_;_])))                // callGeneric function
    | Lambda(_, Lambda(_, Lambda(_, Call(None, m, [_;_;_]))))   // callGeneric2 function
    | Call(None, m, []) -> // value
        m.GetGenericMethodDefinition().MakeGenericMethod(ts)
    | _ -> failwithf "Json.genLetMethod: invalid expr passed: %A" e

let callGeneric (func: Expr<'f -> 'i -> 'o>) (dD: TAttrs -> 'f) ta (targ: Type) : 'i -> 'o =
    let m = genLetMethod(func, [|targ|])
    let dI = dD { ta with Type = targ }
    fun x -> unbox<'o> (m.Invoke(null, [|dI; x|]))

let callGeneric2 (func: Expr<'f -> 'f -> 'i -> 'o>) (dD: TAttrs -> 'f) ta (targ1: Type) (targ2: Type) : 'i -> 'o =
    let m = genLetMethod(func, [|targ1; targ2|])
    let dI1 = dD { ta with Type = targ1 }
    let dI2 = dD { ta with Type = targ2 }
    fun x -> unbox<'o> (m.Invoke(null, [|dI1; dI2; x|]))

let unmakeOption<'T> (dV: obj -> Value) (x: obj) =
    x |> unbox<option<'T>> |> Option.map (box >> dV)

let encodeOptionalField dE ta : obj -> option<Value> =
    if ta.OptionalField then
        ta.Type.GetGenericArguments().[0]
        |> callGeneric <@ unmakeOption @> dE ta
    elif ta.NullableUnion then
        let enc = dE ta
        function null -> None | x -> Some (enc x)
    else
        let enc = dE ta
        fun x -> Some (enc x)

let makeOption<'T> (dV: Value -> obj) (v: option<Value>) =
    match v with
    | Some (Null as v) ->
        // Decode null as None, only if 'T itself doesn't support encoding as Null
        try Some (dV v |> unbox<'T>) with _ -> None
    | v ->
        v |> Option.map (dV >> unbox<'T>)
    |> box

let decodeOptionalField dD ta : option<Value> -> obj =
    if ta.OptionalField then
        ta.Type.GetGenericArguments().[0]
        |> callGeneric <@ makeOption @> dD ta
    elif ta.NullableUnion then
        let dec = dD ta
        function Some v -> dec v | None -> null
    else
        let dec = dD ta
        function Some v -> dec v | None -> raise (DecoderException(Null, ta.Type))

let isInlinableRecordCase (uci: Reflection.UnionCaseInfo) =
    let fields = uci.GetFields()
    fields.Length = 1 &&
    fields.[0].Name = "Item" &&
    FST.IsRecord(fields.[0].PropertyType, flags)

/// Some (Some x) if tagged [<NameUnionCases x>];
/// Some None if tagged [<NamedUnionCases>];
/// None if not tagged.
let getDiscriminatorName (t: Type) =
    t.GetCustomAttributesData()
    |> Seq.tryPick (fun cad ->
        if cad.Constructor.DeclaringType = typeof<NamedUnionCasesAttribute> then
            if cad.ConstructorArguments.Count = 1 then
                Some (Some (cad.ConstructorArguments.[0].Value :?> string))
            else Some None
        else None)

let inferredCasesTable t =
    let allCases =
        FST.GetUnionCases(t, flags)
        |> Array.map (fun c ->
            let fields = c.GetFields()
            let fields =
                if isInlinableRecordCase c then
                    FST.GetRecordFields(fields.[0].PropertyType, flags)
                else fields
            let fields =
                fields
                |> Array.filter (fun f ->
                    let t = f.PropertyType
                    not (t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<option<_>>))
                |> Array.map TAttrs.GetName
            c.Tag, Set fields
        )
        |> Map.ofArray
    let findDistinguishingCase (cases: Map<int, Set<string>>) =
        cases
        |> Map.tryPick (fun t fs ->
            let allOtherFields =
                allCases
                |> Seq.choose (fun (KeyValue(t', fs)) ->
                    if t = t' then None else Some fs)
                |> Set.unionMany
            let uniqueCases = fs - allOtherFields
            if Set.isEmpty uniqueCases then
                None
            else Some (Seq.head uniqueCases, t)
        )
    let rec buildTable acc cases =
        if Map.isEmpty cases then acc else
        match findDistinguishingCase cases with
        | None -> raise (NoDecoderException t)
        | Some (name, tag) ->
            buildTable
                <| (name, tag) :: acc
                <| Map.remove tag cases
    buildTable [] allCases

type TypedNull<'T> = | TypedNull

let MakeTypedNull (t: Type) =
    let t = typedefof<TypedNull<_>>.MakeGenericType(t)
    FSV.MakeUnion(FST.GetUnionCases(t).[0], [||])

let inline GetName x = TAttrs.GetName x

type UnionDiscriminator =
    | NoField of (string * int) list
    | StandardField
    | NamedField of string

type UnionCaseArgFlag =
    | DateTimeFormat of string

[<RequireQualifiedAccess>]
type UnionCaseConstantEncoding =
    | Bool of bool
    | Int of int
    | Float of float
    | String of string
    | Null

type UnionCaseEncoding =
    | Normal of name: string * args: (string * Type * UnionCaseArgFlag[])[]
    | InlineRecord of name: string * record: Type
    | Constant of value: UnionCaseConstantEncoding

let getUnionCaseConstantEncoding (uci: Reflection.UnionCaseInfo) =
    let isNull = 
        uci.DeclaringType.GetCustomAttributesData()
        |> Seq.exists (fun a ->
            a.Constructor.DeclaringType = typeof<CompilationRepresentationAttribute>
            && obj.Equals(a.ConstructorArguments.[0].Value, CompilationRepresentationFlags.UseNullAsTrueValue)
        )
        && (FST.GetUnionCases uci.DeclaringType).Length < 4
        && (FST.GetUnionCases uci.DeclaringType |> Seq.tryFind (fun c -> c.GetFields().Length = 0) = Some uci) 
    if isNull then Some UnionCaseConstantEncoding.Null else
    uci.GetCustomAttributesData()
    |> Seq.tryPick (fun cad ->
        if cad.Constructor.DeclaringType = typeof<ConstantAttribute> then
            let arg = cad.ConstructorArguments.[0]
            if arg.ArgumentType = typeof<int> then
                UnionCaseConstantEncoding.Int (unbox arg.Value)
            elif arg.ArgumentType = typeof<float> then
                UnionCaseConstantEncoding.Float (unbox arg.Value)
            elif arg.ArgumentType = typeof<bool> then
                UnionCaseConstantEncoding.Bool (unbox arg.Value)
            elif arg.ArgumentType = typeof<string> then
                UnionCaseConstantEncoding.String (unbox arg.Value)
            else failwith "Invalid ConstantAttribute."
            |> Some
        else None)

let GetUnionEncoding (t: Type) =
    let discr =
        match getDiscriminatorName t with
        | None -> StandardField
        | Some None -> NoField (inferredCasesTable t)
        | Some (Some n) -> NamedField n
    let cases =
        FST.GetUnionCases(t, flags)
        |> Array.mapi (fun i uci ->
            let name =
                match discr with
                | StandardField -> "$" + string i
                | _ -> GetName uci
            if isInlinableRecordCase uci then
                InlineRecord(name = name, record = uci.GetFields().[0].PropertyType)
            else
                match getUnionCaseConstantEncoding uci with
                | Some e -> Constant e
                | None ->
                    let dateTimeFormats =
                        uci.GetCustomAttributesData()
                        |> Array.ofSeq
                        |> Array.choose (fun cad ->
                            if cad.Constructor.DeclaringType = typeof<DateTimeFormatAttribute> &&
                                cad.ConstructorArguments.Count = 2 then
                                let args = cad.ConstructorArguments
                                Some (args.[0].Value :?> string, args.[1].Value :?> string)
                            else None)
                    let args = uci.GetFields() |> Array.map (fun f ->
                        let flags =
                            dateTimeFormats
                            |> Seq.tryPick (fun (k, v) ->
                                if k = f.Name then Some (DateTimeFormat v) else None)
                            |> Option.toArray
                        GetName f, f.PropertyType, flags)
                    Normal(name = name, args = args))
    discr, cases

let getEncodedUnionFieldName p =
    let n = TAttrs.GetName p
    fun _ -> n

let encodeUnionTag t =
    match getDiscriminatorName t with
    | None -> fun tag -> Some ("$", Number (string tag))
    | Some None -> fun _ -> None
    | Some (Some n) ->
        let tags =
            [|
                for c in FST.GetUnionCases(t, flags) ->
                    String (TAttrs.GetName c)
            |]
        fun tag -> Some (n, tags.[tag])

let unmakeList<'T> (dV: obj -> Value) (x: obj) =
    Array [|
        for v in unbox<list<'T>> x ->
            dV (box v)
    |]

let unionEncoder dE (ta: TAttrs) =
    let t = ta.Type
    let t, isTypedNull =
        if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<TypedNull<_>> then
            t.GetGenericArguments().[0], true
        else t, false
    if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<list<_>>
    then
        t.GetGenericArguments().[0]
        |> callGeneric <@ unmakeList @> dE ta
    elif t = typeof<Value> then
        unbox
    else
    let tR = FSV.PreComputeUnionTagReader(t, flags)
    let cs =
        FST.GetUnionCases(t, flags)
        |> Array.map (fun c ->
            match getUnionCaseConstantEncoding c with
            | Some (UnionCaseConstantEncoding.Int i) -> Choice1Of2 (Number (string i))
            | Some (UnionCaseConstantEncoding.Float f) -> Choice1Of2 (Number (string f))
            | Some (UnionCaseConstantEncoding.Bool b) -> Choice1Of2 (if b then True else False)
            | Some (UnionCaseConstantEncoding.String s) -> Choice1Of2 (String s)
            | Some UnionCaseConstantEncoding.Null -> Choice1Of2 Null
            | None ->
                let r = FSV.PreComputeUnionReader(c, flags)
                let fields = c.GetFields()
                let r, fields =
                    if isInlinableRecordCase c then
                        let rt = fields.[0].PropertyType
                        let rr = FSV.PreComputeRecordReader(rt, flags)
                        let r x = rr (r x).[0]
                        r, FST.GetRecordFields(rt, flags)
                    else r, fields
                let fs = fields |> Array.mapi (fun k f ->
                    let ta = TAttrs.Get(f.PropertyType, f, c)
                    getEncodedUnionFieldName f k, encodeOptionalField dE ta)
                Choice2Of2 (r, fs))
    let encodeTag = encodeUnionTag t
    if isTypedNull then fun _ -> Null else
    fun (x: obj) ->
        match x with
        | null -> Null
        | o when t.IsAssignableFrom(o.GetType()) ->
            let tag = tR o
            match cs.[tag] with
            | Choice1Of2 constant -> constant
            | Choice2Of2 (r, fs) ->
                let data =
                    [|
                        match encodeTag tag with
                        | Some kv -> yield kv
                        | None -> ()
                        for f, d in Array.map2 (fun (f, e) x -> (f, e x)) fs (r o) do
                            if d.IsSome then yield f, d.Value
                    |]
                Object data
        | x ->
            raise EncoderException

let defaultGetUnionTag t =
    let k = FST.GetUnionCases(t, flags).Length
    fun get ->
        match get "$" with
        | Some (Number n) ->
            match Int32.TryParse n with
            | true, tag when tag >= 0 && tag < k -> Some tag
            | _ -> None
        | _ -> None

let inferUnionTag t =
    let findInTable table get =
        table |> List.tryPick (fun (name, tag) ->
            get name |> Option.map (fun _ -> tag))
    findInTable (inferredCasesTable t)

let getUnionTag = fun t ->
    match getDiscriminatorName t with
    | None -> defaultGetUnionTag t
    | Some None -> inferUnionTag t
    | Some (Some n) ->
        let names =
            Map [
                for c in FST.GetUnionCases(t, flags) ->
                    (String (TAttrs.GetName c), c.Tag)
            ]
        fun get ->
            get n |> Option.bind names.TryFind

let makeList<'T> (dV: Value -> obj) = function
    | Array vs -> vs |> Array.map (unbox<'T> << dV) |> List.ofArray |> box
    | x -> raise (DecoderException(x, typeof<list<'T>>))

let unionDecoder dD (ta: TAttrs) =
    let t = ta.Type
    if t.IsGenericType &&
        t.GetGenericTypeDefinition() = typedefof<list<_>>
    then
        t.GetGenericArguments().[0]
        |> callGeneric <@ makeList @> dD ta
    elif t = typeof<Value> then
        box
    else
    let cases = FST.GetUnionCases(t, flags)
    let cs =
        cases
        |> Array.map (fun c ->
            let mk = FSV.PreComputeUnionConstructor(c, flags)
            let fields = c.GetFields()
            let mk, fields =
                if isInlinableRecordCase c then
                    let rt = fields.[0].PropertyType
                    let mkR = FSV.PreComputeRecordConstructor(rt, flags)
                    let mk x = mk [|mkR x|]
                    mk, FST.GetRecordFields(rt, flags)
                else mk, fields
            let fs =
                fields
                |> Array.mapi (fun k f ->
                    let ta = TAttrs.Get(f.PropertyType, f, c)
                    getEncodedUnionFieldName f k, decodeOptionalField dD ta)
            (mk, fs))
    let consts =
        let c =
            cases
            |> Array.choose (fun c ->
                let mk() = FSV.PreComputeUnionConstructor(c, flags) [||]
                getUnionCaseConstantEncoding c
                |> Option.map (function
                    | UnionCaseConstantEncoding.Int i -> (Number (string i), mk())
                    | UnionCaseConstantEncoding.Float f -> (Number (string f), mk())
                    | UnionCaseConstantEncoding.Bool b -> ((if b then True else False), mk())
                    | UnionCaseConstantEncoding.String s -> (String s, mk())
                    | UnionCaseConstantEncoding.Null -> (Null, null)
                )
            )
        let consts = Dictionary()
        for k, v in c do consts.Add(k, v)
        consts
    let getTag = getUnionTag t
    let nullConstant =
        match consts.TryGetValue (String null) with
        | true, x -> x
        | false, _ -> null
    fun (x: Value) ->
        match x with
        | Object fields ->
            let get = table fields
            let tag =
                match getTag get with
                | Some tag -> tag
                | None -> raise (DecoderException(x, ta.Type))
            let (mk, fs) = cs.[tag]
            fs
            |> Array.map (fun (f, e) -> e (get f))
            |> mk
        | Null -> nullConstant
        | v ->
            match consts.TryGetValue v with
            | true, x -> x
            | false, _ -> raise (DecoderException(v, ta.Type))

let fieldFlags = BindingFlags.Instance ||| BindingFlags.Public ||| BindingFlags.NonPublic

let fieldFlagsDeclOnly = fieldFlags ||| BindingFlags.DeclaredOnly

let getObjectFields (t: Type) =
    // FlattenHierarchy flag is not enough to collect
    // backing fields of auto-properties on base classes 
    let getDecl (t: Type) = 
        t.GetFields fieldFlagsDeclOnly
        |> Seq.filter (fun f ->
            let nS =
                f.Attributes &&&
                FieldAttributes.NotSerialized
            int nS = 0
        )
    let rec getAll (t: Type) =
        match t.BaseType with
        | null -> Seq.empty // this is a System.Object
        | b -> Seq.append (getAll b) (getDecl t)
    getAll t |> Array.ofSeq

let getEncodedFieldName t =
    let d = Dictionary()
    let fields =
        if FST.IsRecord(t, flags) then
            FST.GetRecordFields(t, flags)
            |> Seq.cast<MemberInfo>
        else
            getObjectFields t
            |> Seq.cast<MemberInfo>
    for f in fields do
        if not (d.ContainsKey f.Name) then
            d.Add(f.Name, TAttrs.GetName f)
    fun n ->
        match d.TryGetValue n with
        | true, n -> n
        | false, _ -> n

let recordEncoder dE (ta: TAttrs) =
    let t = ta.Type
    let fs =
        FST.GetRecordFields(t, flags)
        |> Array.map (fun f ->
            let r = FSV.PreComputeRecordFieldReader f
            let ta = TAttrs.Get(f.PropertyType, f)
            (getEncodedFieldName t f.Name, r, encodeOptionalField dE ta))
    fun (x: obj) ->
        match x with
        | null ->
            raise EncoderException
        | o when o.GetType() = t ->
            fs
            |> Array.choose (fun (n, r, enc) ->
                enc (r o) |> Option.map (fun e -> (n, e)))
            |> Object
        | _ ->
            raise EncoderException

let recordDecoder dD (ta: TAttrs) =
    let t = ta.Type
    let mk = FSV.PreComputeRecordConstructor(t, flags)
    let fs =
        FST.GetRecordFields(t, flags)
        |> Array.map (fun f ->
            let ta = TAttrs.Get(f.PropertyType, f)
            (getEncodedFieldName t f.Name, decodeOptionalField dD ta))
    fun (x: Value) ->
        match x with
        | Object fields ->
            let get = table fields
            fs
            |> Array.map (fun (n, dec) -> dec (get n))
            |> mk
        | _ ->
            raise (DecoderException(x, ta.Type))

exception NoEncodingException of Type with
    override this.Message =
        "No JSON encoding for " + string this.Data0

type FS = Runtime.Serialization.FormatterServices

let unmakeFlatDictionary<'T> (dE: obj -> Value) (x: obj) =
    Object [|
        for KeyValue(k, v) in unbox<Dictionary<string, 'T>> x ->
            k, dE (box v)
    |]

let unmakeArrayDictionary<'K, 'V when 'K : equality> (dK: obj -> Value) (dV: obj -> Value) (x: obj) =
    Array [|
        for KeyValue(k, v) in unbox<Dictionary<'K, 'V>> x ->
            Array [|dK (box k); dV (box v)|]
    |]

let culture = Globalization.CultureInfo.InvariantCulture
let dtstyle = Globalization.DateTimeStyles.AdjustToUniversal ||| Globalization.DateTimeStyles.AssumeUniversal

let encodeDateTime (ta: TAttrs) =
    let fmt = defaultArg ta.DateTimeFormat "o"
    fun (d: DateTime) -> String (d.ToString(fmt, culture))

let encodeDateTimeOffset =
    fun (d: DateTimeOffset) -> String (d.ToString("o", culture))

let objectEncoder dE (ta: TAttrs) =
    let t = ta.Type
    if t = typeof<DateTime> then
        fun (x: obj) ->
            match x with
            | :? DateTime as t -> encodeDateTime ta t
            | _ -> raise EncoderException
    elif t = typeof<DateTimeOffset> then
        fun (x: obj) ->
            match x with
            | :? DateTimeOffset as t -> encodeDateTimeOffset t
            | _ -> raise EncoderException
    elif t = typeof<unit> then
        fun _ -> Null
    elif t.IsGenericType &&
        t.GetGenericTypeDefinition() = typedefof<Dictionary<_,_>> 
    then
        let ga = t.GetGenericArguments()
        if t.GetGenericArguments().[0] = typeof<string> then
            callGeneric <@ unmakeFlatDictionary @> dE ta ga.[1]
        else
            callGeneric2 <@ unmakeArrayDictionary @> dE ta ga.[0] ga.[1]
    else
    let fs = getObjectFields t
    let ms = fs |> Array.map (fun x -> x :> MemberInfo)
    let es = 
        if t.IsValueType then
            fs |> Array.map (fun f ->
                let ta = TAttrs.Get(f.FieldType, f)
                (getEncodedFieldName f.DeclaringType (f.Name.TrimEnd('@')),
                 encodeOptionalField dE ta))
        else
            fs |> Array.map (fun f ->
                let ta = TAttrs.Get(f.FieldType, f)
                (getEncodedFieldName f.DeclaringType f.Name,
                 encodeOptionalField dE ta))
    fun (x: obj) ->
        match x with
        | null ->
            Null
        | o when t.IsAssignableFrom(o.GetType()) ->
            let data = FS.GetObjectData(o, ms)
            (data, es)
            ||> Array.map2 (fun x (name, enc) ->
                enc x |> Option.map (fun e -> (name, e)))
            |> Array.choose id
            |> Object
        | _ ->
            raise EncoderException

let decodeDateTime (ta: TAttrs) =
    let fmt =
        match ta.DateTimeFormat with
        | Some x -> [|x|]
        // "o" only accepts 7 digits after the seconds,
        // but JavaScript's Date.toISOString() only outputs 3.
        // So we add a custom format to accept that too.
        | None -> [|"o"; @"yyyy-MM-dd\THH:mm:ss.fff\Z"|]
    function
    | String s ->
        match DateTime.TryParseExact(s, fmt, culture, dtstyle) with
        | true, x -> Some x
        | false, _ -> None
    | _ -> None

let decodeDateTimeOffset =
    // "o" only accepts 7 digits after the seconds,
    // but JavaScript's Date.toISOString() only outputs 3.
    // So we add a custom format to accept that too.
    let fmt = [|"o"; @"yyyy-MM-dd\THH:mm:ss.fff\Z"|]
    function
    | String s ->
        match DateTimeOffset.TryParseExact(s, fmt, culture, dtstyle) with
        | true, x -> Some x
        | false, _ -> None
    | _ -> None

let makeFlatDictionary<'T> (dD: Value -> obj) = function
    | Object vs ->
        let d = Dictionary<string, 'T>()
        for k, v in vs do d.Add(k, unbox<'T>(dD v))
        box d
    | x -> raise (DecoderException(x, typeof<Dictionary<string,'T>>))

let makeArrayDictionary<'K, 'V when 'K : equality> (dK: Value -> obj) (dV: Value -> obj) = function
    | Array vs ->
        let d = Dictionary<'K, 'V>()
        for e in vs do
            match e with
            | Array [|k; v|] -> d.Add(unbox<'K>(dK k), unbox<'V>(dV v))
            | x -> raise (DecoderException(x, typeof<Dictionary<'K,'V>>))
        box d
    | x -> raise (DecoderException(x, typeof<Dictionary<'K,'V>>))

let rec decodeObj value =
    match value with
    | Null -> null
    | True -> box true
    | False -> box false
    | Number x ->
        match Int32.TryParse x with
        | true, n -> box n
        | false, _ ->
            match Double.TryParse x with
            | true, f -> box f
            | false, _ -> raise (DecoderException(value, typeof<obj>))
    | String s -> box s
    | Array xs ->
        box [| for x in xs -> decodeObj x |]
    | Object xs ->
        let d = Dictionary()
        for k, v in xs do d.Add(k, decodeObj v)
        box d

let objectDecoder dD (ta: TAttrs) =
    let t = ta.Type
    if t = typeof<DateTime> then
        fun (x: Value) ->
            match decodeDateTime ta x with
            | Some d -> box d
            | None -> raise (DecoderException(x, typeof<DateTime>))
    elif t = typeof<DateTimeOffset> then
        fun (x: Value) ->
            match decodeDateTimeOffset x with
            | Some d -> box d
            | None -> raise (DecoderException(x, typeof<DateTime>))
    elif t = typeof<unit> then
        function
        | Null -> box ()
        | x -> raise (DecoderException(x, typeof<unit>))
    elif t.IsGenericType &&
        t.GetGenericTypeDefinition() = typedefof<Dictionary<_,_>>
    then
        let ga = t.GetGenericArguments()
        if t.GetGenericArguments().[0] = typeof<string> then
            callGeneric <@ makeFlatDictionary @> dD ta ga.[1]
        else
            callGeneric2 <@ makeArrayDictionary @> dD ta ga.[0] ga.[1]
    elif t = typeof<obj> then
        decodeObj
    elif t.IsValueType then
        let fs = t.GetFields fieldFlags
        match t.GetConstructor (fs |> Array.map (fun f -> f.FieldType)) with
        | null -> raise (NoEncodingException t)
        | _ ->
        let ds = fs |> Array.map (fun f ->
            let ta = TAttrs.Get(f.FieldType, f)
            (getEncodedFieldName f.DeclaringType (f.Name.TrimEnd('@')),
             decodeOptionalField dD ta))
        fun (x: Value) ->
            match x with
            | Object fields ->
                let get = table fields
                let data =
                    ds
                    |> Seq.map (fun (n, dec) ->
                       dec (get n))
                    |> Seq.toArray
                Activator.CreateInstance(t, args = data)
            | x ->
                raise (DecoderException(x, ta.Type))
    else
    match t.GetConstructor [||] with
    | null -> raise (NoEncodingException t)
    | _ -> ()
    let fs = getObjectFields t
    let ms = fs |> Array.map (fun x -> x :> MemberInfo)
    let ds = fs |> Array.map (fun f ->
        let ta = TAttrs.Get(f.FieldType, f)
        (getEncodedFieldName f.DeclaringType f.Name,
         decodeOptionalField dD ta))
    fun (x: Value) ->
        match x with
        | Null -> null
        | Object fields ->
            let get = table fields
            let obj = Activator.CreateInstance t
            let data =
                ds
                |> Seq.map (fun (n, dec) ->
                   dec (get n))
                |> Seq.toArray
            FS.PopulateObjectMembers(obj, ms, data)
        | x ->
            raise (DecoderException(x, ta.Type))

let unmakeFlatMap<'T> (dV: obj -> Value)  (x: obj) =
    Object [|
        for KeyValue(k, v) in unbox<Map<string, 'T>> x ->
            k, dV (box v)
    |]

let unmakeArrayMap<'K, 'T when 'K : comparison> (dK: obj -> Value) (dV: obj -> Value) (x: obj) =
    Array [|
        for KeyValue(k, v) in unbox<Map<'K, 'T>> x ->
            Array [| dK (box k); dV (box v) |]
    |]

let mapEncoder dE (ta: TAttrs) =
    let t = ta.Type
    let tg = t.GetGenericArguments()
    if tg.Length <> 2 then raise EncoderException
    if tg.[0] = typeof<string> then
        callGeneric <@ unmakeFlatMap @> dE ta tg.[1]
    else
        callGeneric2 <@ unmakeArrayMap @> dE ta tg.[0] tg.[1]

/// Decode a Map<string, _> from { key: value, ... } JSON object
let makeFlatMap<'T> (dV: Value -> obj) = function
    | Object vs ->
        Map.ofArray<string, 'T>(
            vs |> Array.map (fun (k, v) -> k, unbox<'T> (dV v))
        )
        |> box
    | x -> raise (DecoderException(x, typeof<Map<string, 'T>>))

/// Decode a Map<_, _> from [ [key, value], ... ] JSON object
let makeArrayMap<'K, 'V when 'K : comparison> (dK: Value -> obj) (dV: Value -> obj) = function
    | Array vs ->
        Map.ofArray<'K, 'V>(
            vs |> Array.map (function
            | Array [|k; v|] -> unbox<'K> (dK k), unbox<'V> (dV v)
            | x -> raise (DecoderException(x, typeof<Map<'K, 'V>>)))
        )
        |> box
    | x -> raise (DecoderException(x, typeof<Map<'K, 'V>>))

let mapDecoder dD (ta: TAttrs) =
    let t = ta.Type
    let tg = t.GetGenericArguments()
    if tg.[0] = typeof<string> then
        callGeneric <@ makeFlatMap @> dD ta tg.[1]
    else
        callGeneric2 <@ makeArrayMap @> dD ta tg.[0] tg.[1]

let unmakeSet<'T when 'T : comparison> (dV: obj -> Value) (x: obj) =
    Array [|for v in unbox<Set<'T>> x -> dV v|]

let setEncoder dE (ta: TAttrs) =
    let t = ta.Type
    let tg = t.GetGenericArguments()
    if tg.Length <> 1 then raise EncoderException
    callGeneric <@ unmakeSet @> dE ta tg.[0]

let unmakeResizeArray<'T when 'T : comparison> (dV: obj -> Value) (x: obj) =
    Array [|for v in unbox<ResizeArray<'T>> x -> dV v|]

let resizeArrayEncoder dE (ta: TAttrs) =
    let t = ta.Type
    let tg = t.GetGenericArguments()
    if tg.Length <> 1 then raise EncoderException
    callGeneric <@ unmakeResizeArray @> dE ta tg.[0]

let unmakeQueue<'T when 'T : comparison> (dV: obj -> Value) (x: obj) =
    Array [|for v in unbox<Queue<'T>> x -> dV v|]

let queueEncoder dE (ta: TAttrs) =
    let t = ta.Type
    let tg = t.GetGenericArguments()
    if tg.Length <> 1 then raise EncoderException
    callGeneric <@ unmakeQueue @> dE ta tg.[0]

let unmakeStack<'T when 'T : comparison> (dV: obj -> Value) (x: obj) =
    Array [|for v in unbox<Stack<'T>> x -> dV v|]

let stackEncoder dE (ta: TAttrs) =
    let t = ta.Type
    let tg = t.GetGenericArguments()
    if tg.Length <> 1 then raise EncoderException
    callGeneric <@ unmakeStack @> dE ta tg.[0]

let unmakeLinkedList<'T when 'T : comparison> (dV: obj -> Value) (x: obj) =
    Array [|for v in unbox<LinkedList<'T>> x -> dV v|]

let linkedListEncoder dE (ta: TAttrs) =
    let t = ta.Type
    let tg = t.GetGenericArguments()
    if tg.Length <> 1 then raise EncoderException
    callGeneric <@ unmakeLinkedList @> dE ta tg.[0]

let unmakeNullable<'T when 'T: (new: unit -> 'T) and 'T: struct and 'T :> ValueType> (dV: obj -> Value) (x: obj) =
    if obj.ReferenceEquals(x, null) then Null else dV x    
           
let nbleEncoder dE (ta: TAttrs) =
    let t = ta.Type
    let tg = t.GetGenericArguments()
    if tg.Length <> 1 then raise EncoderException
    callGeneric <@ unmakeNullable @> dE ta tg.[0]

let makeSet<'T when 'T : comparison> (dV: Value -> obj) = function
    | Array vs ->
        Set.ofArray<'T>(vs |> Array.map (unbox<'T> << dV))
        |> box
    | x -> raise (DecoderException(x, typeof<Set<'T>>))

let makeSet'<'T when 'T : comparison> (dV: Value -> obj) (xs: seq<obj>) =
    Set.ofSeq (Seq.cast<'T> xs)
    |> box

let setDecoder dD (ta: TAttrs) =
    let t = ta.Type
    let tg = t.GetGenericArguments()
    if tg.Length <> 1 then raise EncoderException
    callGeneric <@ makeSet @> dD ta tg.[0]

let makeResizeArray<'T when 'T : comparison> (dV: Value -> obj) = function
    | Array vs ->
        ResizeArray(vs |> Seq.map (unbox<'T> << dV))
        |> box
    | x -> raise (DecoderException(x, typeof<ResizeArray<'T>>))

let resizeArrayDecoder dD (ta: TAttrs) =
    let t = ta.Type
    let tg = t.GetGenericArguments()
    if tg.Length <> 1 then raise EncoderException
    callGeneric <@ makeResizeArray @> dD ta tg.[0]

let makeQueue<'T when 'T : comparison> (dV: Value -> obj) = function
    | Array vs ->
        Queue(vs |> Seq.map (unbox<'T> << dV))
        |> box
    | x -> raise (DecoderException(x, typeof<Queue<'T>>))

let queueDecoder dD (ta: TAttrs) =
    let t = ta.Type
    let tg = t.GetGenericArguments()
    if tg.Length <> 1 then raise EncoderException
    callGeneric <@ makeQueue @> dD ta tg.[0]

let makeStack<'T when 'T : comparison> (dV: Value -> obj) = function
    | Array vs ->
        Stack(vs |> Array.map (unbox<'T> << dV) |> Array.rev)
        |> box
    | x -> raise (DecoderException(x, typeof<Stack<'T>>))

let stackDecoder dD (ta: TAttrs) =
    let t = ta.Type
    let tg = t.GetGenericArguments()
    if tg.Length <> 1 then raise EncoderException
    callGeneric <@ makeStack @> dD ta tg.[0]

let makeLinkedList<'T when 'T : comparison> (dV: Value -> obj) = function
    | Array vs ->
        LinkedList(vs |> Seq.map (unbox<'T> << dV))
        |> box
    | x -> raise (DecoderException(x, typeof<LinkedList<'T>>))

let linkedListDecoder dD (ta: TAttrs) =
    let t = ta.Type
    let tg = t.GetGenericArguments()
    if tg.Length <> 1 then raise EncoderException
    callGeneric <@ makeLinkedList @> dD ta tg.[0]

let makeNullable<'T when 'T: (new: unit -> 'T) and 'T: struct and 'T :> ValueType> (dV: Value -> obj) =
    function
        | Null -> null
        | x -> box (Nullable(unbox<'T> (dV x)))

let nbleDecoder dD (ta: TAttrs) =
    let t = ta.Type
    let tg = t.GetGenericArguments()
    if tg.Length <> 1 then raise EncoderException
    callGeneric <@ makeNullable @> dD ta tg.[0]

let enumEncoder dE (ta: TAttrs) =
    let t = ta.Type
    let uT = Enum.GetUnderlyingType t
    let uE = dE { ta with Type = uT }
    fun (x: obj) ->
        uE (Convert.ChangeType(x, uT)) : Value

let enumDecoder dD (ta: TAttrs) =
    let t = ta.Type
    let uT = Enum.GetUnderlyingType t
    let uD = dD { ta with Type = uT }
    fun (x: Value) ->
        let y : obj = uD x
        Enum.ToObject(t, y)

type TypeEncoding<'a, 'b> = (TAttrs -> 'a -> 'b) -> TAttrs -> 'a -> 'b

type Encodings<'a, 'b> =
    {
        Scalar: Serializer -> 'a -> 'b
        Array: TypeEncoding<'a, 'b>
        Tuple: TypeEncoding<'a, 'b>
        Union: TypeEncoding<'a, 'b>
        Record: TypeEncoding<'a, 'b>
        Enum: TypeEncoding<'a, 'b>
        Map: TypeEncoding<'a, 'b>
        Set: TypeEncoding<'a, 'b>
        ResizeArray: TypeEncoding<'a, 'b>
        Queue: TypeEncoding<'a, 'b>
        Stack: TypeEncoding<'a, 'b>
        LinkedList: TypeEncoding<'a, 'b>
        Nullable: TypeEncoding<'a, 'b>
        Object: TypeEncoding<'a, 'b>
    }

module Encodings =

    let Decode =
        {
            Scalar = fun { Decode = x } -> x
            Array = arrayDecoder
            Tuple = tupleDecoder
            Union = unionDecoder
            Record = recordDecoder
            Enum = enumDecoder
            Map = mapDecoder
            Set = setDecoder
            ResizeArray = resizeArrayDecoder
            Queue = queueDecoder
            Stack = stackDecoder
            LinkedList = linkedListDecoder
            Nullable = nbleDecoder
            Object = objectDecoder
        }

    let Encode =
        {
            Scalar = fun { Encode = x } -> x
            Array = arrayEncoder
            Tuple = tupleEncoder
            Union = unionEncoder
            Record = recordEncoder
            Enum = enumEncoder
            Map = mapEncoder
            Set = setEncoder
            ResizeArray = resizeArrayEncoder
            Queue = queueEncoder
            Stack = stackEncoder
            LinkedList = linkedListEncoder
            Nullable = nbleEncoder
            Object = objectEncoder
        }

    let private defaultof (t: Type) =
        if t.IsValueType then
            Activator.CreateInstance(t)
        else null

    let Dummy =
        {
            Scalar = fun _ -> defaultof
            Array = fun dD ta ->
                let x = box (Array.CreateInstance(ta.Type.GetElementType(), 0))
                fun _ -> x
            Tuple = fun dD ta ->
                let xs = FST.GetTupleElements ta.Type |> Array.map (fun t -> dD (TAttrs.Get(t)) t)
                let x = FSV.MakeTuple(xs, ta.Type)
                fun _ -> x
            Union = fun dD ta ->
                let uci = FST.GetUnionCases(ta.Type, flags).[0]
                let xs = uci.GetFields() |> Array.map (fun f -> dD (TAttrs.Get(f.PropertyType, f)) f.PropertyType)
                let x = FSV.MakeUnion(uci, xs, flags)
                fun _ -> x
            Record = fun dD ta ->
                let xs = FST.GetRecordFields(ta.Type, flags) |> Array.map (fun f -> dD (TAttrs.Get(f.PropertyType, f)) f.PropertyType)
                let x = FSV.MakeRecord(ta.Type, xs, flags)
                fun _ -> x
            Enum = fun dD ta ->
                let x = defaultof ta.Type
                fun _ -> x
            Map = fun dD ta ->
                let x = genLetMethod(<@ Map.empty @>, ta.Type.GetGenericArguments()).Invoke(null, [||])
                fun _ -> x
            Set = fun dD ta ->
                let x = genLetMethod(<@ Set.empty @>, ta.Type.GetGenericArguments()).Invoke(null, [||])
                fun _ -> x
            ResizeArray = fun dD ta _ -> null
            Queue = fun dD ta _ -> null
            Stack = fun dD ta _ -> null
            LinkedList = fun dD ta _ -> null
            Nullable = fun dD ta _ -> null
            Object = fun _ _ _ -> null
        }

let getEncoding e wrap (cache: ConcurrentDictionary<_,_>) =
    let rec get (ta: TAttrs) =
        let derive dD =
            try
                if ta.Type.IsArray then
                    if ta.Type.GetArrayRank() = 1 then
                        e.Array dD ta
                    else raise (NoEncodingException ta.Type)
                elif FST.IsTuple ta.Type then
                    e.Tuple dD ta
                elif FST.IsUnion (ta.Type, flags) then
                    e.Union dD ta
                elif FST.IsRecord (ta.Type, flags) then
                    e.Record dD ta
                elif ta.Type.IsEnum then
                    e.Enum dD ta
                else
                    let tn =
                        if ta.Type.IsGenericType 
                        then Some (ta.Type.GetGenericTypeDefinition().FullName)
                        else None
                    match tn with
                    | Some "Microsoft.FSharp.Collections.FSharpMap`2" -> e.Map dD ta
                    | Some "Microsoft.FSharp.Collections.FSharpSet`1" -> e.Set dD ta
                    | Some "System.Collections.Generic.List`1" -> e.ResizeArray dD ta
                    | Some "System.Collections.Generic.Queue`1" -> e.Queue dD ta
                    | Some "System.Collections.Generic.Stack`1" -> e.Stack dD ta
                    | Some "System.Collections.Generic.LinkedList`1" -> e.LinkedList dD ta
                    | Some "System.Nullable`1" -> e.Nullable dD ta
                    | _ -> 
                        e.Object dD ta
            with
            | NoEncodingException t ->
                reraise()
            | e ->
                fun _ -> raise (Exception("Error during RPC JSON conversion", e))
        if ta.Type = null then raise (NoEncodingException ta.Type) else
            match serializers.TryGetValue ta.Type with
            | true, x -> e.Scalar x
            | _ ->
                let newRef = ref Unchecked.defaultof<_>
                lock newRef <| fun () ->
                let r = cache.GetOrAdd(ta, newRef)
                if obj.ReferenceEquals(r, newRef) then
                    let d = derive (wrap get)
                    r := d
                    d
                else
                    let d = !r
                    // inside recursive types, delay the lookup of the function
                    if obj.ReferenceEquals(d, null) then
                        fun x -> 
                            let d = !r
                            // another thread might be running derive for the type
                            // we wait for the lock to release only in this case
                            if obj.ReferenceEquals(d, null) then
                                let d = lock r <| fun () -> !r
                                d x 
                            else d x    
                    else d
    get

let baseTAttrs = ConcurrentDictionary<Type, TAttrs>()
let decoders = ConcurrentDictionary()
let encoders = ConcurrentDictionary()

let getBaseTAttrs (t: Type) =
    baseTAttrs.GetOrAdd(t, fun t -> TAttrs.Get(t))

let getDefaultBuilder =
    getEncoding Encodings.Dummy id (ConcurrentDictionary<_,_>())

let getDecoder =
    getEncoding Encodings.Decode id decoders

let getEncoder =
    getEncoding Encodings.Encode
        (fun dE ta ->
            if ta.Type.IsSealed || FST.IsUnion(ta.Type, flags) then dE ta else
            fun x -> dE (if x = null then ta else { ta with Type = x.GetType() }) x)
        encoders

let GetDecoder (t: Type) : Decoder<obj> =
    getDecoder (getBaseTAttrs t)

let GetEncoder (t: Type) : Encoder<obj> =
    getEncoder (getBaseTAttrs t)

let Decode<'T> : Decoder<'T> =
    let d = GetDecoder typeof<'T>
    fun x -> d x :?> 'T

let Encode<'T> : Encoder<'T> =
    let e = GetEncoder typeof<'T>
    fun x -> e (box x)

let BuildDefaultValueFor (t: Type) =
    getDefaultBuilder (getBaseTAttrs t) t

let BuildDefaultValue<'T>() =
    BuildDefaultValueFor typeof<'T> :?> 'T

let Read<'T> (tr: TextReader) =
    Raw.Read tr |> Decode<'T>

let Deserialize<'T> (s: string) =
    Raw.Parse s |> Decode<'T>

let Write<'T> (tr: TextWriter) (value: 'T) =
    Raw.Write tr <| Encode<'T> value

let Serialize<'T> (value: 'T) =
    Raw.Stringify <| Encode<'T> value
