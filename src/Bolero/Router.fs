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

namespace Bolero

#nowarn "40" // recursive value `segment` in getSegment

open System
open System.Collections.Generic
open System.Diagnostics.CodeAnalysis
open System.Net
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Text
open FSharp.Reflection
open Microsoft.FSharp.Core.CompilerServices

/// <summary>A router that binds page navigation with Elmish.</summary>
/// <typeparam name="model">The Elmish model type.</typeparam>
/// <typeparam name="msg">The Elmish message type.</typeparam>
/// <category>Routing</category>
type IRouter<'model, 'msg> =
    /// <summary>Get the uri corresponding to <paramref name="model" />.</summary>
    abstract GetRoute : model: 'model -> string

    /// <summary>Get the message to send when the page navigates to <paramref name="uri" />.</summary>
    abstract SetRoute : uri: string -> option<'msg>

    /// <summary>
    /// The message to send if the user initially navigates to an unknown uri.
    /// If None, don't send a message and stay on the initial page.
    /// </summary>
    abstract NotFound : option<'msg>

/// <summary>A simple hand-written router.</summary>
/// <typeparam name="model">The Elmish model type.</typeparam>
/// <typeparam name="msg">The Elmish message type.</typeparam>
/// <category>Routing</category>
type Router<'model, 'msg> =
    {
        /// <summary>Get the uri corresponding to the model.</summary>
        getRoute: 'model -> string
        /// <summary>Get the message to send when the page navigates to the uri.</summary>
        setRoute: string -> option<'msg>
    }

    interface IRouter<'model, 'msg> with
        member this.GetRoute(model) = this.getRoute model
        member this.SetRoute(uri) = this.setRoute uri
        member this.NotFound = None

/// <summary>A simple router where the endpoint corresponds to a value easily gettable from the model.</summary>
/// <typeparam name="ep">The routing endpoint type.</typeparam>
/// <typeparam name="model">The Elmish model type.</typeparam>
/// <typeparam name="msg">The Elmish message type.</typeparam>
/// <category>Routing</category>
type Router<'ep, 'model, 'msg> =
    {
        /// <summary>Extract the current endpoint from the model.</summary>
        getEndPoint: 'model -> 'ep
        /// <summary>Get the uri corresponding to an endpoint.</summary>
        getRoute: 'ep -> string
        /// <summary>Get the message to send when the page navigates to an uri.</summary>
        setRoute: string -> option<'ep>
        /// <summary>Convert an endpoint into the message that sets it.</summary>
        makeMessage: 'ep -> 'msg
        /// <summary>
        /// The endpoint to switch to if the user initially navigates to an unknown uri.
        /// If None, stay on the initial page.
        /// </summary>
        notFound: option<'ep>
    }

    /// <summary>Get the uri for the given <paramref name="endpoint" />.</summary>
    member this.Link(endpoint, [<Optional>] hash: string) =
        let link = this.getRoute endpoint
        match hash with
        | null -> link
        | hash -> link + "#" + hash

    interface IRouter<'model, 'msg> with
        member this.GetRoute(model) = this.getRoute (this.getEndPoint model)
        member this.SetRoute(uri) = this.setRoute uri |> Option.map this.makeMessage
        member this.NotFound = Option.map this.makeMessage this.notFound

/// <summary>Declare how an F# union case matches to a URI.</summary>
/// <category>Routing</category>
[<AttributeUsage(AttributeTargets.Property, AllowMultiple = false)>]
type EndPointAttribute(path, query) =
    inherit Attribute()

    /// <summary>Declare how an F# union case matches to a URI.</summary>
    /// <param name="endpoint">The endpoint URI path and query.</param>
    new (endpoint: string) =
        let path, query =
            match endpoint.IndexOf('?') with
            | -1 -> endpoint, ""
            | n -> endpoint[..n-1], endpoint[n+1..]
        EndPointAttribute(path.Trim('/').Split('/'),query)

    /// The path that this endpoint recognizes.
    member this.Path = path

    /// The query string that this endpoint recognizes.
    member this.Query = query

/// <summary>
/// Declare that the given field of an F# union case matches the entire remainder of the URL path.
/// </summary>
/// <category>Routing</category>
[<AttributeUsage(AttributeTargets.Property, AllowMultiple = false)>]
type WildcardAttribute
    /// <summary>
    /// Declare that the given field of an F# union case matches the entire remainder of the URL path.
    /// </summary>
    /// <param name="field">The name of the field. If unspecified, this applies to the last field of the case.</param>
    ([<Optional>] field: string) =
    inherit Attribute()

    /// <summary>The name of the field.</summary>
    member this.Field = field

/// <summary>The kinds of invalid router.</summary>
/// <category>Routing</category>
[<RequireQualifiedAccess>]
type InvalidRouterKind =
    | UnsupportedType of Type
    | ParameterSyntax of UnionCaseInfo * string
    | DuplicateField of UnionCaseInfo * string
    | UnknownField of UnionCaseInfo * string
    | MissingField of UnionCaseInfo * string
    | ParameterTypeMismatch of UnionCaseInfo * string * UnionCaseInfo * string
    | ModifierMismatch of UnionCaseInfo * string * UnionCaseInfo * string
    | IdenticalPath of UnionCaseInfo * UnionCaseInfo
    | RestNotLast of UnionCaseInfo
    | InvalidRestType of UnionCaseInfo
    | MultiplePageModels of UnionCaseInfo

/// <summary>Exception thrown when a router is incorrectly defined.</summary>
/// <category>Routing</category>
exception InvalidRouter of kind: InvalidRouterKind with
    override this.Message =
        let withCase (case: UnionCaseInfo) s =
            $"Invalid router defined for union case {case.DeclaringType.FullName}.{case.Name}: %s{s}"
        match this.kind with
        | InvalidRouterKind.UnsupportedType ty ->
            "Unsupported route type: " + ty.FullName
        | InvalidRouterKind.ParameterSyntax(case, field) ->
            withCase case $"Invalid parameter syntax: {field}"
        | InvalidRouterKind.DuplicateField(case, field) ->
            withCase case $"Field duplicated in the path: {field}"
        | InvalidRouterKind.UnknownField(case, field) ->
            withCase case $"Unknown field in the path: {field}"
        | InvalidRouterKind.MissingField(case, field) ->
            withCase case $"Missing field in the path: {field}"
        | InvalidRouterKind.ParameterTypeMismatch(case, field, otherCase, otherField) ->
            withCase case $"Parameter {field} at the same path position as {otherCase.Name}'s {otherField} but has a different type"
        | InvalidRouterKind.ModifierMismatch(case, field, otherCase, otherField) ->
            withCase case $"Parameter {field} at the same path position as {otherCase.Name}'s {otherField} but has a different modifier"
        | InvalidRouterKind.IdenticalPath(case, otherCase) ->
            withCase case $"Matches the exact same path as {otherCase.Name}"
        | InvalidRouterKind.RestNotLast case ->
            withCase case "{*rest} parameter must be the last fragment"
        | InvalidRouterKind.InvalidRestType case ->
            withCase case "{*rest} parameter must have type string, list or array"
        | InvalidRouterKind.MultiplePageModels case ->
            withCase case "multiple page models on the same case"

/// <summary>A wrapper type to include a model in a router page type.</summary>
/// <seealso href="https://fsbolero.io/docs/Routing#page-models" />
/// <category>Routing</category>
[<CLIMutable>]
type PageModel<'T> =
    { Model: 'T }

#if NET8_0
    static let prop = typeof<PageModel<'T>>.GetProperty("Model")

    member internal this.SetModel(value) =
        prop.SetValue(this, value)
#else
    member internal this.SetModel(value) =
        (Unsafe.AsRef<'T>(&this.Model)) <- value
#endif

[<AutoOpen>]
module private RouterImpl =
    open System.Text.RegularExpressions

    type SingleParser = string -> option<obj>
    type SingleWriter = obj -> option<string>
    type SingleSerializer =
        {
            parse: SingleParser
            write: SingleWriter
        }

    type SegmentParserResult = option<obj * list<string>>
    type SegmentParser = list<string> -> Map<string, string> -> SegmentParserResult
    type SegmentWriter = obj -> list<string> * Map<string, string>
    type SegmentSerializer =
        {
            parse: SegmentParser
            write: SegmentWriter
        }

    let fail kind = raise (InvalidRouter kind)

    let inline tryParseBaseType<'T when 'T : (static member TryParse : string * byref<'T> -> bool)> s =
        let mutable out = Unchecked.defaultof<'T>
        if (^T : (static member TryParse : string * byref<'T> -> bool) (s, &out)) then
            Some (box out)
        else
            None

    let inline baseTypeSingleSerializer<'T when 'T : (static member TryParse : string * byref<'T> -> bool)> () : SingleSerializer =
        {
            parse = tryParseBaseType<'T>
            write = string >> Some
        }

    let singleSegmentSerializer (s: SingleSerializer) : SegmentSerializer =
        {
            parse = fun path _ ->
                match path with
                | [] -> None
                | x :: rest ->
                    match s.parse x with
                    | Some x -> Some (x, rest)
                    | None -> None
            write = fun x -> Option.toList (s.write x), Map.empty
        }

    let baseTypeSingleSerializers : IDictionary<Type, SingleSerializer> = dict [
        typeof<string>, {
            parse = fun x -> Some (box (WebUtility.UrlDecode x))
            write = fun x -> Some (WebUtility.UrlEncode (unbox x))
        }
        typeof<bool>, {
            parse = tryParseBaseType<bool>
            // `string true` returns capitalized "True", but we want lowercase "true".
            write = fun x -> Some (if unbox x then "true" else "false")
        }
        typeof<Byte>, baseTypeSingleSerializer<Byte>()
        typeof<SByte>, baseTypeSingleSerializer<SByte>()
        typeof<Int16>, baseTypeSingleSerializer<Int16>()
        typeof<UInt16>, baseTypeSingleSerializer<UInt16>()
        typeof<Int32>, baseTypeSingleSerializer<Int32>()
        typeof<UInt32>, baseTypeSingleSerializer<UInt32>()
        typeof<Int64>, baseTypeSingleSerializer<Int64>()
        typeof<UInt64>, baseTypeSingleSerializer<UInt64>()
        typeof<single>, baseTypeSingleSerializer<single>()
        typeof<float>, baseTypeSingleSerializer<float>()
        typeof<decimal>, baseTypeSingleSerializer<decimal>()
    ]

    let baseTypeSegmentSerializers : IDictionary<Type, SegmentSerializer> = dict [
        for KeyValue(k, s) in baseTypeSingleSerializers do
            k, singleSegmentSerializer s
    ]

    let getSingleSerializer (ty: Type) : SingleSerializer * obj voption =
        match baseTypeSingleSerializers.TryGetValue(ty) with
        | true, s -> s, ValueNone
        | false, _ ->
            if ty.IsGenericType &&
               (let gen = ty.GetGenericTypeDefinition()
                gen = typedefof<option<_>> || gen = typedefof<voption<_>>)
            then
                match baseTypeSingleSerializers.TryGetValue(ty.GetGenericArguments()[0]) with
                | true, s ->
                    let cases = FSharpType.GetUnionCases(ty)
                    let noneCase = cases[0]
                    let someCase = cases[1]
                    let someCtor = FSharpValue.PreComputeUnionConstructor(someCase)
                    let someDector = FSharpValue.PreComputeUnionReader(someCase)
                    let none = FSharpValue.MakeUnion(noneCase, Array.empty)
                    let getTag = FSharpValue.PreComputeUnionTagReader(ty)
                    {
                        parse = s.parse >> Option.map (fun x -> someCtor [|x|])
                        write = fun x ->
                            if getTag x = 0 then
                                None
                            else
                                s.write (someDector x).[0]
                    }, ValueSome none
                | false, _ -> fail (InvalidRouterKind.UnsupportedType ty)
            else
                fail (InvalidRouterKind.UnsupportedType ty)

    let merge (map1: Map<'k, 'v>) (map2: Map<'k, 'v>) =
        Map.foldBack Map.add map1 map2

    let sequenceSegment getSegment (ty: Type) revAndConvert toListAndLength : SegmentSerializer =
        let itemSegment = getSegment ty
        let rec parse acc remainingLength fragments query =
            if remainingLength = 0 then
                Some (revAndConvert acc, fragments)
            else
                match itemSegment.parse fragments query with
                | None -> None
                | Some (x, rest) ->
                    parse (x :: acc) (remainingLength - 1) rest query
        {
            parse = fun path query ->
                match path with
                | x :: rest ->
                    match Int32.TryParse(x) with
                    | true, length -> parse [] length rest query
                    | false, _ -> None
                | _ -> None
            write = fun x ->
                let list, (length: int) = toListAndLength x
                let path, query =
                    (Map.empty, list)
                    ||> List.mapFold (fun query item ->
                        let segments, itemQuery = itemSegment.write item
                        segments, merge itemQuery query)
                (string length :: List.concat path), query
        }

    let [<Literal>] FLAGS_STATIC =
        Reflection.BindingFlags.Static |||
        Reflection.BindingFlags.Public |||
        Reflection.BindingFlags.NonPublic

    let arrayRevAndUnbox<'T> (l: list<obj>) : 'T[] =
        let a = [|for x in l -> unbox<'T> x|]
        Array.Reverse(a)
        a

    let arrayLengthAndBox<'T> (a: array<'T>) : list<obj> * int =
        [for x in a -> box x], a.Length

    let arraySegment getSegment ty : SegmentSerializer =
        let arrayRevAndUnbox =
            typeof<SegmentSerializer>.DeclaringType.GetMethod("arrayRevAndUnbox", FLAGS_STATIC)
                .MakeGenericMethod([|ty|])
        let arrayLengthAndBox =
            typeof<SegmentSerializer>.DeclaringType.GetMethod("arrayLengthAndBox", FLAGS_STATIC)
                .MakeGenericMethod([|ty|])
        sequenceSegment getSegment ty
            (fun l -> arrayRevAndUnbox.Invoke(null, [|l|]))
            (fun l -> arrayLengthAndBox.Invoke(null, [|l|]) :?> _)

    let listRevAndUnbox<'T> (l: list<obj>) : list<'T> =
        List.map unbox<'T> l |> List.rev

    let listLengthAndBox<'T> (l: list<'T>) : list<obj> * int =
        List.mapFold (fun l e -> box e, l + 1) 0 l

    let listSegment getSegment ty : SegmentSerializer =
        let listRevAndUnbox =
            typeof<SegmentSerializer>.DeclaringType.GetMethod("listRevAndUnbox", FLAGS_STATIC)
                .MakeGenericMethod([|ty|])
        let listLengthAndBox =
            typeof<SegmentSerializer>.DeclaringType.GetMethod("listLengthAndBox", FLAGS_STATIC)
                .MakeGenericMethod([|ty|])
        sequenceSegment getSegment ty
            (fun l -> listRevAndUnbox.Invoke(null, [|l|]))
            (fun l -> listLengthAndBox.Invoke(null, [|l|]) :?> _)

    [<CustomEquality; NoComparison>]
    type ParameterModifier =
        /// No modifier: "/{parameter}"
        | Basic
        /// Rest of the path: "/{*parameter}"
        | Rest of (seq<obj> -> obj) * (obj -> seq<obj>)
        // Optional segment: "/{?parameter}" (TODO)
        //| Optional

        interface IEquatable<ParameterModifier> with
            member this.Equals(that) =
                match this, that with
                | Basic, Basic
                | Rest _, Rest _ -> true
                | _ -> false

    /// A {parameter} path segment.
    type SegmentParameter =
        {
            /// A parameter can be common among multiple union cases.
            /// `index` lists these cases, and for each of them, its total number of fields and the index of the field for this segment.
            index: list<UnionCaseInfo * int * int>
            ``type``: Type
            segment: SegmentSerializer
            modifier: ParameterModifier
            /// Note that several cases can have the same parameter with different names.
            /// In this case, the name field is taken from the first declared case.
            name: string
        }

    /// Intermediate representation of a path segment.
    type UnionParserSegment =
        | Constant of string
        | Parameter of SegmentParameter

    type QueryParameter =
        {
            index: int
            serializer: SingleSerializer
            optionalDefaultValue: obj voption
            name: string
            propName: string
        }

    type UnionCase =
        {
            info: UnionCaseInfo
            ctor: obj[] -> obj
            argCount: int
            segments: UnionParserSegment list
            query: QueryParameter list
        }

    /// The parser for a union type at a given point in the path.
    type UnionParser =
        {
            /// All recognized "/constant" segments, associated with the parser for the rest of the path.
            constants: IDictionary<string, UnionParser>
            /// The recognized "/{parameter}" segment, if any.
            parameter: option<SegmentParameter * UnionParser>
            /// The union case that parses correctly if the path ends here, if any.
            finalize: option<UnionCase>
        }

    let parseEndPointCasePathAndQuery (case: UnionCaseInfo) : list<string> * string =
        case.GetCustomAttributes(typeof<EndPointAttribute>)
        |> Array.tryPick (function
            | :? EndPointAttribute as e -> Some (List.ofSeq e.Path, e.Query)
            | _ -> None)
        |> Option.defaultWith (fun () -> [case.Name], "")

    let isConstantFragment (s: string) =
        not (s.Contains("{"))

    type Unboxer =
        static member List<'T> (items: seq<obj>) : list<'T> =
            [ for x in items -> unbox<'T> x ]

        static member Array<'T> (items: seq<obj>) : 'T[] =
            [| for x in items -> unbox<'T> x |]

    type Decons =
        static member List<'T> (l: list<'T>) : seq<obj> =
            Seq.cast l

        static member Array<'T> (l: 'T[]) : seq<obj> =
            Seq.cast l

    let restModifierFor (ty: Type) case =
        if ty = typeof<string> then
            ty, Rest(
                Seq.cast<string> >> String.concat "/" >> box,
                fun s ->
                    match unbox<string> s with
                    | "" -> Seq.empty
                    | s -> s.Split('/') |> Seq.cast<obj>
            )
        elif ty.IsArray && ty.GetArrayRank() = 1 then
            let elt = ty.GetElementType()
            let unboxer = typeof<Unboxer>.GetMethod("Array", FLAGS_STATIC).MakeGenericMethod([|elt|])
            let decons = typeof<Decons>.GetMethod("Array", FLAGS_STATIC).MakeGenericMethod([|elt|])
            elt, Rest(
                (fun x -> unboxer.Invoke(null, [|x|])),
                (fun x -> decons.Invoke(null, [|x|]) :?> _)
            )
        elif ty.IsGenericType && ty.GetGenericTypeDefinition() = typedefof<list<_>> then
            let targs = ty.GetGenericArguments()
            let unboxer = typeof<Unboxer>.GetMethod("List", FLAGS_STATIC).MakeGenericMethod(targs)
            let decons = typeof<Decons>.GetMethod("List", FLAGS_STATIC).MakeGenericMethod(targs)
            targs[0], Rest(
                (fun x -> unboxer.Invoke(null, [|x|])),
                (fun x -> decons.Invoke(null, [|x|]) :?> _)
            )
        else
            fail (InvalidRouterKind.InvalidRestType case)

    let fragmentParameterRE = Regex(@"^\{([*]?)([a-zA-Z0-9_]+)\}$", RegexOptions.Compiled)
    let queryParameterRE = Regex(@"^\{([a-zA-Z0-9_]+)\}$", RegexOptions.Compiled)

    let isPageModel (ty: Type) =
        ty.IsGenericType && ty.GetGenericTypeDefinition() = typedefof<PageModel<_>>

    let findPageModel (case: UnionCaseInfo) =
        ((0, None), case.GetFields())
        ||> Array.fold (fun (i, found) field ->
            i + 1,
            if isPageModel field.PropertyType then
                match found with
                | None -> Some (i, field.PropertyType)
                | Some _ -> fail (InvalidRouterKind.MultiplePageModels case)
            else
                found)
        |> snd

    let getCtor (defaultPageModel: obj -> unit) (case: UnionCaseInfo) =
        let ctor = FSharpValue.PreComputeUnionConstructor(case, true)
        match findPageModel case with
        | None -> ctor
        | Some (i, ty) ->
            let fields = case.GetFields()
            let dummyArgs = Array.zeroCreate fields.Length
            fields |> Array.iteri (fun i field ->
                if field.PropertyType.IsValueType then
                    dummyArgs[i] <- Activator.CreateInstance(field.PropertyType, true))
            let model = FSharpValue.MakeRecord(ty, [|null|])
            dummyArgs[i] <- model
            let dummy = ctor dummyArgs
            defaultPageModel dummy
            fun vals ->
                vals[i] <- model
                ctor vals

    let parseQueryParameters (case: UnionCaseInfo) (query: string) : QueryParameter list =
        let fields = case.GetFields()
        query.Split('&', StringSplitOptions.RemoveEmptyEntries)
        |> Seq.map (fun q ->
            let paramName, propName =
                match q.IndexOf('=') with
                | -1 ->
                    let name = queryParameterRE.Match(q).Groups[1].Value
                    name, name
                | n ->
                    let name = q[..n-1]
                    name, queryParameterRE.Match(q[n+1..]).Groups[1].Value
            let index =
                fields
                |> Array.tryFindIndex (fun f -> f.Name = propName)
                |> Option.defaultWith(fun () -> fail (InvalidRouterKind.UnknownField(case, propName)))
            let prop = fields[index]
            let serializer, optionalDefaultValue = getSingleSerializer prop.PropertyType
            {
                index = index
                serializer = serializer
                optionalDefaultValue = optionalDefaultValue
                name = paramName
                propName = propName
            })
        |> List.ofSeq

    let parseEndPointCase getSegment (defaultPageModel: obj -> unit) (case: UnionCaseInfo) =
        let ctor = getCtor defaultPageModel case
        let fields = case.GetFields()
        let path, query = parseEndPointCasePathAndQuery case
        let query = parseQueryParameters case query
        let defaultFrags() =
            fields
            |> Array.mapi (fun i p ->
                let ty = p.PropertyType
                if isPageModel ty then None
                elif query |> List.exists (fun q -> q.propName = p.Name) then None else
                Some <| Parameter {
                    index = [case, fields.Length, i]
                    ``type`` = ty
                    segment = getSegment ty
                    modifier = Basic
                    name = p.Name
                })
            |> Array.choose id
            |> List.ofSeq
        match path with
        // EndPoint "/"
        | [] -> { info = case; ctor = ctor; argCount = fields.Length; segments = defaultFrags(); query = query }
        // EndPoint "/const"
        | [root] when isConstantFragment root ->
            { info = case; ctor = ctor; argCount = fields.Length; segments = Constant root :: defaultFrags(); query = query }
        // EndPoint <complex_path>
        | frags ->
            let unboundFields =
                fields
                |> Array.choose (fun f -> if isPageModel f.PropertyType || query |> List.exists (fun q -> q.propName = f.Name) then None else Some f.Name)
                |> HashSet
            let fragCount = frags.Length
            let segments =
                frags
                |> List.mapi (fun fragIx frag ->
                    if isConstantFragment frag then
                        Constant frag
                    else
                        let m = fragmentParameterRE.Match(frag)
                        if not m.Success then fail (InvalidRouterKind.ParameterSyntax(case, frag))
                        let fieldName = m.Groups[2].Value
                        match fields |> Array.tryFindIndex (fun p -> p.Name = fieldName) with
                        | Some i ->
                            let p = fields[i]
                            if not (unboundFields.Remove(fieldName)) then
                                fail (InvalidRouterKind.DuplicateField(case, fieldName))
                            let ty = p.PropertyType
                            let eltTy, modifier =
                                match m.Groups[1].Value with
                                | "" -> ty, Basic
                                | "*" ->
                                    if fragIx <> fragCount - 1 then
                                        fail (InvalidRouterKind.RestNotLast case)
                                    restModifierFor ty case
                                | _ -> fail (InvalidRouterKind.ParameterSyntax(case, frag))
                            Parameter {
                                index = [case, fields.Length, i]
                                ``type`` = ty
                                segment = getSegment eltTy
                                modifier = modifier
                                name = p.Name
                            }
                        | None -> fail (InvalidRouterKind.UnknownField(case, fieldName))
                )
            if unboundFields.Count > 0 then
                fail (InvalidRouterKind.MissingField(case, Seq.head unboundFields))
            { info = case; ctor = ctor; argCount = fields.Length; segments = segments; query = query }

    let rec mergeEndPointCaseFragments (cases: seq<UnionCase>) : UnionParser =
        let constants = Dictionary<string, _>()
        let mutable parameter = None
        let mutable final = None
        cases |> Seq.iter (fun case ->
            match case.segments with
            | Constant s :: rest ->
                let existing =
                    match constants.TryGetValue(s) with
                    | true, x -> x
                    | false, _ -> []
                constants[s] <- { case with segments = rest } :: existing
            | Parameter param :: rest ->
                match parameter with
                | Some (case', param': SegmentParameter, ps) ->
                    if param.``type`` <> param'.``type`` then
                        fail (InvalidRouterKind.ParameterTypeMismatch(case', param'.name, case.info, param.name))
                    if param.modifier <> param'.modifier then
                        fail (InvalidRouterKind.ModifierMismatch(case', param'.name, case.info, param.name))
                    let param = { param with index = param.index @ param'.index }
                    parameter <- Some (case', param, { case with segments = rest } :: ps)
                | None ->
                    parameter <- Some (case.info, param, [{ case with segments = rest }])
            | [] ->
                match final with
                | Some case' -> fail (InvalidRouterKind.IdenticalPath(case.info, case'.info))
                | None -> final <- Some case
        )
        {
            constants = dict [
                for KeyValue(s, cases) in constants do
                    yield s, mergeEndPointCaseFragments cases
            ]
            parameter = parameter |> Option.map (fun (_, param, cases) ->
                param, mergeEndPointCaseFragments cases)
            finalize = final
        }

    let parseUnion cases : SegmentParser =
        let parser = mergeEndPointCaseFragments cases
        fun l ->
            let d = Dictionary<UnionCaseInfo, obj[]>()
            let rec run (parser: UnionParser) segments query =
                let finalize rest =
                    parser.finalize |> Option.bind (fun case ->
                        let args =
                            match d.TryGetValue(case.info) with
                            | true, args -> args
                            | false, _ -> Array.zeroCreate case.argCount
                        let allQueryParamsAreHere =
                            case.query
                            |> List.forall (fun p ->
                                match Map.tryFind p.name query with
                                | None ->
                                    match p.optionalDefaultValue with
                                    | ValueSome def ->
                                        args[p.index] <- def
                                        true
                                    | ValueNone -> false
                                | Some v ->
                                    match p.serializer.parse v with
                                    | Some x ->
                                        args[p.index] <- x
                                        true
                                    | None -> false)
                        if allQueryParamsAreHere then
                            Some (case.ctor args, rest)
                        else None)
                let mutable constant = Unchecked.defaultof<_>
                match segments with
                | s :: rest when parser.constants.TryGetValue(s, &constant) ->
                    run constant rest query
                | segments ->
                    parser.parameter
                    |> Option.bind (function
                        | { modifier = Basic } as param, nextParser ->
                            match param.segment.parse segments query with
                            | None -> None
                            | Some (o, rest) ->
                                for case, fieldCount, i in param.index do
                                    let a =
                                        match d.TryGetValue(case) with
                                        | true, a -> a
                                        | false, _ ->
                                            let a = Array.zeroCreate fieldCount
                                            d[case] <- a
                                            a
                                    a[i] <- o
                                run nextParser rest query
                        | { modifier = Rest(restBuild, _) } as param, nextParser ->
                            let restValues = ResizeArray()
                            let rec parse segments =
                                match param.segment.parse segments query, segments with
                                | None, [] ->
                                    for case, fieldCount, i in param.index do
                                        let a =
                                            match d.TryGetValue(case) with
                                            | true, a -> a
                                            | false, _ ->
                                                let a = Array.zeroCreate fieldCount
                                                d[case] <- a
                                                a
                                        a[i] <- restBuild restValues
                                    run nextParser [] query
                                | None, _::_ -> None
                                | Some (o, rest), _ ->
                                    restValues.Add(o)
                                    parse rest
                            parse segments
                    )
                |> Option.orElseWith (fun () -> finalize segments)
            run parser l

    let parseConsecutiveTypes getSegment (tys: Type[]) (ctor: obj[] -> obj) : SegmentParser =
        let fields = Array.map getSegment tys
        fun (fragments: list<string>) query ->
            let args = Array.zeroCreate fields.Length
            let rec go i fragments =
                if i = fields.Length then
                    Some (ctor args, fragments)
                else
                    match fields[i].parse fragments query with
                    | None -> None
                    | Some (x, rest) ->
                        args[i] <- x
                        go (i + 1) rest
            go 0 fragments

    let writeConsecutiveTypes getSegment (tys: Type[]) (dector: obj -> obj[]) : SegmentWriter =
        let fields = tys |> Array.map (fun t -> (getSegment t).write)
        fun (r: obj) ->
            let mutable segments = ListCollector()
            let query =
                (Map.empty, fields, dector r)
                |||> Array.fold2 (fun query field item ->
                    let itemSegments, itemQuery = field item
                    segments.AddMany(itemSegments)
                    merge itemQuery query)
            segments.Close(), query

    let caseDector (case: UnionCaseInfo) : obj -> obj[] =
        FSharpValue.PreComputeUnionReader(case, true)

    let writeUnionCase (case: UnionCase) =
        let dector = caseDector case.info
        fun o ->
            let vals = dector o
            let mutable segments = ListCollector()
            let query =
                case.query
                |> Seq.choose (fun param ->
                    param.serializer.write vals[param.index]
                    |> Option.map (fun s -> param.name, s))
                |> Map
            let query =
                (query, case.segments)
                ||> List.fold (fun query item ->
                    match item with
                    | Constant s ->
                        segments.Add(s)
                        query
                    | Parameter({ modifier = Basic } as param) ->
                        let _, _, i = param.index |> List.find (fun (case', _, _) -> case' = case.info)
                        let itemSegments, itemQuery = param.segment.write vals[i]
                        segments.AddMany(itemSegments)
                        merge itemQuery query
                    | Parameter({ modifier = Rest(_, decons) } as param) ->
                        let _, _, i = param.index |> List.find (fun (case', _, _) -> case' = case.info)
                        (query, decons vals[i])
                        ||> Seq.fold (fun query x ->
                            let itemSegments, itemQuery = param.segment.write x
                            segments.AddMany(itemSegments)
                            merge itemQuery query)
                )
            segments.Close(), query

    let unionSegment (getSegment: Type -> SegmentSerializer) (defaultPageModel: obj -> unit) (ty: Type) : SegmentSerializer =
        let cases =
            FSharpType.GetUnionCases(ty, true)
            |> Array.map (parseEndPointCase getSegment defaultPageModel)
        let write =
            let writers = Array.map writeUnionCase cases
            let tagReader = FSharpValue.PreComputeUnionTagReader(ty, true)
            fun r -> writers[tagReader r] r
        let parse = parseUnion cases
        { parse = parse; write = write }

    let tupleSegment getSegment ty =
        let tys = FSharpType.GetTupleElements ty
        let ctor = FSharpValue.PreComputeTupleConstructor ty
        let dector = FSharpValue.PreComputeTupleReader ty
        {
            parse = parseConsecutiveTypes getSegment tys ctor
            write = writeConsecutiveTypes getSegment tys dector
        }

    let recordSegment getSegment ty =
        let tys = FSharpType.GetRecordFields(ty, true) |> Array.map (fun p -> p.PropertyType)
        let ctor = FSharpValue.PreComputeRecordConstructor(ty, true)
        let dector = FSharpValue.PreComputeRecordReader(ty, true)
        {
            parse = parseConsecutiveTypes getSegment tys ctor
            write = writeConsecutiveTypes getSegment tys dector
        }

    let rec getSegment (cache: Dictionary<Type, SegmentSerializer>) (defaultPageModel: obj -> unit) (ty: Type) : SegmentSerializer =
        match cache.TryGetValue(ty) with
        | true, x -> unbox x
        | false, _ ->
            // Add lazy version in case ty is recursive.
            let rec segment = ref {
                parse = fun x -> segment.Value.parse x
                write = fun x -> segment.Value.write x
            }
            cache[ty] <- segment.Value
            let getSegment = getSegment cache ignore
            segment.Value <-
                if ty.IsArray && ty.GetArrayRank() = 1 then
                    arraySegment getSegment (ty.GetElementType())
                elif ty.IsGenericType && ty.GetGenericTypeDefinition() = typedefof<list<_>> then
                    listSegment getSegment (ty.GetGenericArguments()[0])
                elif FSharpType.IsUnion(ty, true) then
                    unionSegment getSegment defaultPageModel ty
                elif FSharpType.IsTuple(ty) then
                    tupleSegment getSegment ty
                elif FSharpType.IsRecord(ty, true) then
                    recordSegment getSegment ty
                else
                    fail (InvalidRouterKind.UnsupportedType ty)
            cache[ty] <- segment.Value
            segment.Value

    let splitPathAndQuery (pathAndQuery: string) : string list * Map<string, string> =
        match pathAndQuery.IndexOf('?') with
        | -1 -> pathAndQuery.Split('/') |> List.ofArray, Map.empty
        | n ->
            let path = pathAndQuery[..n-1].Split('/') |> List.ofArray
            let query =
                pathAndQuery[n+1..].Split('&')
                |> Seq.map (fun s ->
                    match s.IndexOf('=') with
                    | -1 -> s, ""
                    | n -> s[..n-1], s[n+1..])
                |> Map
            path, query

/// <summary>Functions for building Routers that bind page navigation with Elmish.</summary>
/// <category>Routing</category>
module Router =

    /// <summary>
    /// Infer a router constructed around an endpoint type <typeparamref name="ep" />.
    /// This type must be an F# union type, and its cases should use <see cref="T:EndPointAttribute" />
    /// to declare how they match to a URI.
    /// </summary>
    /// <param name="makeMessage">Function that creates the message for switching to the page pointed by an endpoint.</param>
    /// <param name="getEndPoint">Function that extracts the current endpoint from the Elmish model.</param>
    /// <param name="defaultPageModel">
    /// Function that indicates the default <see cref="T:PageModel`1" /> for a given endpoint.
    /// Inside this function, call <see cref="M:definePageModel" /> to indicate the page model to use when switching to a new page.
    /// </param>
    /// <returns>A router for the given endpoint type.</returns>
    let inferWithModel<[<DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)>] 'ep, 'model, 'msg>
            (makeMessage: 'ep -> 'msg) (getEndPoint: 'model -> 'ep) (defaultPageModel: 'ep -> unit) =
        let ty = typeof<'ep>
        let cache = Dictionary()
        for KeyValue(k, v) in baseTypeSegmentSerializers do cache.Add(k, v)
        let frag = getSegment cache (unbox >> defaultPageModel) ty
        {
            getEndPoint = getEndPoint
            getRoute = fun ep ->
                let segments, query = frag.write (box ep)
                let path = String.concat "/" segments
                if Map.isEmpty query then
                    path
                else
                    let sb = StringBuilder(path)
                    query
                    |> Seq.iteri (fun i (KeyValue(k, v)) ->
                        sb.Append(if i = 0 then '?' else '&')
                            .Append(k)
                            .Append('=')
                            .Append(v)
                        |> ignore)
                    sb.ToString()
            setRoute = fun path ->
                splitPathAndQuery path
                ||> frag.parse
                |> Option.bind (function
                    | x, [] -> Some (unbox<'ep> x)
                    | _ -> None)
            makeMessage = makeMessage
            notFound = None
        }

    /// <summary>
    /// Infer a router constructed around an endpoint type <typeparamref name="ep" />.
    /// This type must be an F# union type, and its cases should use <see cref="T:EndPointAttribute" />
    /// to declare how they match to a URI.
    /// </summary>
    /// <param name="makeMessage">Function that creates the message for switching to the page pointed by an endpoint.</param>
    /// <param name="getEndPoint">Function that extracts the current endpoint from the Elmish model.</param>
    /// <returns>A router for the given endpoint type.</returns>
    let infer<[<DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)>] 'ep, 'model, 'msg>
            (makeMessage: 'ep -> 'msg) (getEndPoint: 'model -> 'ep) =
        inferWithModel makeMessage getEndPoint ignore

    /// <summary>
    /// Indicate the endpoint to switch to if the user initially navigates to an unknown uri.
    /// </summary>
    let withNotFound (notFound: 'ep) (router: Router<'ep, 'model, 'msg>) =
        { router with notFound = Some notFound }

    /// <summary>
    /// Indicate the message to send if the user initially navigates to an unknown uri.
    /// </summary>
    let withNotFoundMsg (notFound: 'msg) (router: IRouter<'model, 'msg>) =
        { new IRouter<'model, 'msg> with
            member _.GetRoute(model) = router.GetRoute(model)
            member _.SetRoute(uri) = router.SetRoute(uri)
            member _.NotFound = Some notFound }

    /// <summary>
    /// An empty PageModel. Used when constructing an endpoint to pass to methods such as <see cref="M:Router`3.Link" />.
    /// </summary>
    let noModel<'T> = { Model = Unchecked.defaultof<'T> }

    /// <summary>
    /// Define the PageModel for a given endpoint.
    /// Must be called inside the <c>defaultPageModel</c> function passed to <see cref="M:inferWithModel`3" />.
    /// </summary>
    /// <param name="pageModel">
    /// The PageModel, retrieved from the endpoint passed to the function by <see cref="M:inferWithModel`3" />.
    /// </param>
    /// <param name="value">The value of the page model to put inside <paramref name="pageModel" />.</param>
    let definePageModel (pageModel: PageModel<'T>) (value: 'T) =
        pageModel.SetModel(value)

/// <category>Routing</category>
[<Extension>]
type RouterExtensions =

    /// <summary>Create an HTML href attribute pointing to the given endpoint.</summary>
    /// <param name="this">The router.</param>
    /// <param name="endpoint">The router endpoint.</param>
    /// <param name="hash">The hash part of the URL, to scroll to the element with this id.</param>
    /// <returns>An <c>href</c> attribute pointing to the given endpoint.</returns>
    [<Extension>]
    static member HRef(this: Router<'ep, _, _>, endpoint: 'ep, [<Optional>] hash: string) : Attr =
        Attr(fun _ tb i ->
            tb.AddAttribute(i, "href", this.Link(endpoint, hash))
            i + 1)
