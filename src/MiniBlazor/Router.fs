namespace MiniBlazor

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

    let inline private tryParseBaseType<'T when 'T : (static member TryParse : string * byref<'T> -> bool)> () =
        fun s ->
            let mutable out = Unchecked.defaultof<'T>
            if (^T : (static member TryParse : string * byref<'T> -> bool) (s, &out)) then
                Some (box out)
            else
                None

    let private baseTypes : IDictionary<Type, string -> option<obj>> = dict [
        typeof<string>, box >> Some
        typeof<bool>, tryParseBaseType<bool>()
        typeof<Byte>, tryParseBaseType<Byte>()
        typeof<SByte>, tryParseBaseType<SByte>()
        typeof<Int16>, tryParseBaseType<Int16>()
        typeof<UInt16>, tryParseBaseType<UInt16>()
        typeof<Int32>, tryParseBaseType<Int32>()
        typeof<UInt32>, tryParseBaseType<UInt32>()
        typeof<Int64>, tryParseBaseType<Int64>()
        typeof<UInt64>, tryParseBaseType<UInt64>()
        typeof<single>, tryParseBaseType<single>()
        typeof<float>, tryParseBaseType<float>()
        typeof<decimal>, tryParseBaseType<decimal>()
    ]

    let private parseEndPointCasePath (case: UnionCaseInfo) =
        case.GetCustomAttributes()
        |> Array.tryPick (function
            | :? EndPointAttribute as e -> Some e.Root
            | _ -> None)
        |> Option.defaultWith (fun () -> case.Name)

    let private makeFragmentsParser (case: UnionCaseInfo) =
        let fields =
            case.GetFields()
            |> Array.map (fun field ->
                match baseTypes.TryGetValue field.PropertyType with
                | true, f -> f
                | false, _ ->
                    failwithf "Router.Infer: unsupported union field type: %s in %s"
                        field.PropertyType.FullName case.Name
            )
        let ctor = FSharpValue.PreComputeUnionConstructor case
        fun (fragments: ArraySegment<string>) ->
            if fragments.Count <> fields.Length then None else
            let args = Array.zeroCreate fields.Length
            let rec go i =
                if i = fields.Length then
                    Some (ctor args)
                else
                    match fields.[i] fragments.[i] with
                    | Some x ->
                        args.[i] <- x
                        go (i + 1)
                    | None -> None
            go 0

    let private makeFragmentsWriter (path: string) (case: UnionCaseInfo) =
        let reader = FSharpValue.PreComputeUnionReader(case, true)
        fun (r: obj) ->
            let b = StringBuilder()
            let args = reader r
            if path <> "" then
                b.Append(path) |> ignore
            args |> Array.iteri (fun i arg ->
                if i > 0 || path <> "" then
                    b.Append('/') |> ignore
                b.Append(arg.ToString()) |> ignore
            )
            b.ToString()

    /// Infer a router constructed around an endpoint type `'ep`.
    /// This type must be an F# union type, and its cases should use `EndPointAttribute`
    /// to declare how they match to a URI.
    let infer<'ep, 'model, 'msg> (makeMessage: 'ep -> 'msg) (getEndPoint: 'model -> 'ep) =
        let ty = typeof<'ep>
        if not (FSharpType.IsUnion ty) then
            failwithf "Router.Infer used with %s, which is not an F# union type." ty.FullName
        let cases =
            FSharpType.GetUnionCases(ty, true)
            |> Array.map (fun case ->
                let path = parseEndPointCasePath case
                path, makeFragmentsParser case, makeFragmentsWriter path case)
        let parsers =
            cases
            |> Array.map (fun (path, parser, _) -> path, parser)
            |> dict
        let getRoute =
            let tagReader = FSharpValue.PreComputeUnionTagReader(ty, true)
            fun r ->
                let _, _, f = cases.[tagReader r]
                f r
        let setRoute (s: string) =
            let fragments = s.Split('/')
            match parsers.TryGetValue fragments.[0] with
            | true, c ->
                ArraySegment(fragments, 1, fragments.Length - 1)
                |> c
                |> Option.map (unbox<'ep> >> makeMessage)
            | false, _ ->
                match parsers.TryGetValue "" with
                | true, c ->
                    ArraySegment(fragments)
                    |> c
                    |> Option.map (unbox<'ep> >> makeMessage)
                | false, _ ->
                    None
        {
            getEndPoint = getEndPoint
            getRoute = getRoute
            setRoute = setRoute
        }

[<Extension>]
type RouterExtensions =

    [<Extension>]
    static member HRef(this: Router<'ep, _, _>, endpoint: 'ep) : Attr =
        "href", box (this.Link endpoint)
