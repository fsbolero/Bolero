namespace Bolero

open System
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Text
open FSharp.Reflection

/// A router that binds page navigation with Elmish.
type IRouter<'model, 'msg> =
    /// Get the uri corresponding to `model`.
    abstract GetRoute : model: 'model -> string

    /// Get the message to send when the page navigates to `uri`.
    abstract SetRoute : uri: string -> option<'msg>

/// A simple hand-written router.
type Router<'model, 'msg> =
    {
        /// Get the uri corresponding to `model`.
        getRoute: 'model -> string
        /// Get the message to send when the page navigates to `uri`.
        setRoute: string -> option<'msg>
    }

    interface IRouter<'model, 'msg> with
        member this.GetRoute(model) = this.getRoute model
        member this.SetRoute(uri) = this.setRoute uri

/// A simple router where the endpoint corresponds to a value easily gettable from the model.
type Router<'ep, 'model, 'msg> =
    {
        getEndPoint: 'model -> 'ep
        getRoute: 'ep -> string
        setRoute: string -> option<'msg>
    }

    /// Get the uri for the given endpoint.
    member this.Link(ep) = this.getRoute ep

    interface IRouter<'model, 'msg> with
        member this.GetRoute(model) = this.getRoute (this.getEndPoint model)
        member this.SetRoute(uri) = this.setRoute uri

/// Declare how an F# union case matches to a URI.
[<AttributeUsage(AttributeTargets.Property, AllowMultiple = false)>]
type EndPointAttribute(endpoint: string) =
    inherit Attribute()

    let endpoint = endpoint.Trim('/')

    /// The root path fragment that this endpoint recognizes.
    member this.Root = endpoint

/// Functions for building Routers that bind page navigation with Elmish.
module Router =

    type ArraySegment<'T> with
        member this.Item with get(i) = this.Array.[this.Offset + i]

    type private SegmentParserResult = seq<obj * list<string>>
    type private SegmentParser = list<string> -> SegmentParserResult
    type private SegmentWriter = obj -> list<string>
    type private Segment =
        {
            parse: SegmentParser
            write: SegmentWriter
        }

    let private fail : SegmentParserResult = Seq.empty
    let private ok x : SegmentParserResult = Seq.singleton x

    let inline private tryParseBaseType<'T when 'T : (static member TryParse : string * byref<'T> -> bool)> () =
        fun s ->
            let mutable out = Unchecked.defaultof<'T>
            if (^T : (static member TryParse : string * byref<'T> -> bool) (s, &out)) then
                Some (box out)
            else
                None

    let inline private baseTypeSegment<'T when 'T : (static member TryParse : string * byref<'T> -> bool)> () =
        {
            parse = function
                | [] -> fail
                | x :: rest ->
                    match tryParseBaseType<'T>() x with
                    | Some x -> ok (box x, rest)
                    | None -> fail
            write = fun x -> [string x]
        }

    let private baseTypes : IDictionary<Type, Segment> = dict [
        typeof<string>, {
            parse = function
                | [] -> fail
                | x :: rest -> ok (box x, rest)
            write = unbox<string> >> List.singleton
        }
        typeof<bool>, baseTypeSegment<bool>()
        typeof<Byte>, baseTypeSegment<Byte>()
        typeof<SByte>, baseTypeSegment<SByte>()
        typeof<Int16>, baseTypeSegment<Int16>()
        typeof<UInt16>, baseTypeSegment<UInt16>()
        typeof<Int32>, baseTypeSegment<Int32>()
        typeof<UInt32>, baseTypeSegment<UInt32>()
        typeof<Int64>, baseTypeSegment<Int64>()
        typeof<UInt64>, baseTypeSegment<UInt64>()
        typeof<single>, baseTypeSegment<single>()
        typeof<float>, baseTypeSegment<float>()
        typeof<decimal>, baseTypeSegment<decimal>()
    ]

    let private parseEndPointCasePath (case: UnionCaseInfo) =
        case.GetCustomAttributes()
        |> Array.tryPick (function
            | :? EndPointAttribute as e -> Some e.Root
            | _ -> None)
        |> Option.defaultWith (fun () -> case.Name)

    let private parseConsecutiveTypes getSegment (tys: Type[]) (ctor: obj[] -> obj) : SegmentParser =
        let fields = Array.map getSegment tys
        fun (fragments: list<string>) ->
            let args = Array.zeroCreate fields.Length
            let rec go i fragments =
                if i = fields.Length then
                    ok (ctor args, fragments)
                else
                    fields.[i].parse fragments
                    |> Seq.collect (fun (x, rest) ->
                        args.[i] <- x
                        go (i + 1) rest
                    )
            go 0 fragments

    let private writeConsecutiveTypes getSegment (tys: Type[]) (dector: obj -> obj[]) : SegmentWriter =
        let fields = tys |> Array.map (fun t -> (getSegment t).write)
        fun (r: obj) ->
            Array.map2 (<|) fields (dector r)
            |> List.concat

    let private parseUnionCaseArgs getSegment (case: UnionCaseInfo) : SegmentParser =
        let tys = case.GetFields() |> Array.map (fun p -> p.PropertyType)
        let ctor = FSharpValue.PreComputeUnionConstructor case
        parseConsecutiveTypes getSegment tys ctor

    let private writeUnionCase getSegment (path: string) (case: UnionCaseInfo) =
        let tys = case.GetFields() |> Array.map (fun p -> p.PropertyType)
        let dector = FSharpValue.PreComputeUnionReader(case, true)
        let write = writeConsecutiveTypes getSegment tys dector
        match path with
        | "" -> write
        | path -> fun r -> path :: write r

    let private unionSegment (getSegment: Type -> Segment) (ty: Type) : Segment =
        let parsers, readers =
            FSharpType.GetUnionCases(ty, true)
            |> Array.map (fun case ->
                let path = parseEndPointCasePath case
                (path, parseUnionCaseArgs getSegment case), writeUnionCase getSegment path case)
            |> Array.unzip
        let parsers = dict parsers
        let getRoute =
            let tagReader = FSharpValue.PreComputeUnionTagReader(ty, true)
            fun r -> readers.[tagReader r] r
        let unprefixedSetRoute path =
            match parsers.TryGetValue "" with
            | true, c -> c path
            | false, _ -> fail
        let setRoute path =
            seq {
                match path with
                | head :: rest ->
                    match parsers.TryGetValue head with
                    | true, c -> yield! c rest
                    | false, _ -> ()
                | [] -> ()
                yield! unprefixedSetRoute path
            }
        { parse = setRoute; write = getRoute }

    let private tupleSegment getSegment ty =
        let tys = FSharpType.GetTupleElements ty
        let ctor = FSharpValue.PreComputeTupleConstructor ty
        let dector = FSharpValue.PreComputeTupleReader ty
        {
            parse = parseConsecutiveTypes getSegment tys ctor
            write = writeConsecutiveTypes getSegment tys dector
        }

    let rec private getSegment (cache: Dictionary<Type, Segment>) (ty: Type) : Segment =
        let getSegment = getSegment cache
        match cache.TryGetValue(ty) with
        | true, x -> unbox x
        | false, _ ->
            let segment =
                if FSharpType.IsUnion(ty, true) then
                    unionSegment getSegment ty
                elif FSharpType.IsTuple(ty) then
                    tupleSegment getSegment ty
                else
                    failwithf "Router.Infer used with %s, which is not an F# union type." ty.FullName
            cache.[ty] <- segment
            segment

    /// Infer a router constructed around an endpoint type `'ep`.
    /// This type must be an F# union type, and its cases should use `EndPointAttribute`
    /// to declare how they match to a URI.
    let infer<'ep, 'model, 'msg> (makeMessage: 'ep -> 'msg) (getEndPoint: 'model -> 'ep) =
        let ty = typeof<'ep>
        let cache = Dictionary()
        for KeyValue(k, v) in baseTypes do cache.Add(k, v)
        let frag = getSegment cache ty
        {
            getEndPoint = getEndPoint
            getRoute = fun ep ->
                box ep
                |> frag.write
                |> String.concat "/"
            setRoute = fun path ->
                path.Split('/')
                |> List.ofArray
                |> frag.parse
                |> Seq.tryPick (function
                    | x, [] -> Some (unbox<'ep> x |> makeMessage)
                    | _ -> None)
        }

[<Extension>]
type RouterExtensions =

    [<Extension>]
    static member HRef(this: Router<'ep, _, _>, endpoint: 'ep) : Attr =
        Attr("href", this.Link endpoint)
