namespace Bolero.Remoting

open System
open System.IO
open System.Net.Http
open System.Runtime.CompilerServices
open System.Text
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.DependencyInjection.Extensions
open FSharp.Reflection
open Bolero

/// Provides remote service implementations when running in WebAssembly.
type ClientRemoteProvider(http: HttpClient) =

    let normalizeBasePath(basePath: string) =
        basePath + (if basePath.EndsWith "/" then "" else "/")

    let send (method: HttpMethod) (requestUri: string) (content: obj) =
        let content =
            match content with
            | null ->
                Json.Raw.Stringify Json.Null
            | content ->
                Json.GetEncoder (content.GetType()) content
                |> Json.Raw.Stringify
        new HttpRequestMessage(method, requestUri,
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        )
        |> http.SendAsync
        |> Async.AwaitTask

    member this.SendAndParse<'T>(method, requestUri, content) = async {
        let! resp = send method requestUri content
        let! respBody = resp.Content.ReadAsStreamAsync() |> Async.AwaitTask
        use reader = new StreamReader(respBody)
        return Json.Read<'T> reader
    }

    member this.MakeRemoteProxy(ty: Type, baseUri: string ref) =
        match RemotingExtensions.ExtractRemoteMethods(ty) with
        | Error errors ->
            raise <| AggregateException(
                "Cannot create remoting handler for type " + ty.FullName,
                [| for e in errors -> exn e |])
        | Ok methods ->
            let ctor = FSharpValue.PreComputeRecordConstructor(ty, true)
            methods
            |> Array.map (fun method ->
                let post =
                    typeof<ClientRemoteProvider>.GetMethod("SendAndParse")
                        .MakeGenericMethod([|method.ReturnType|])
                FSharpValue.MakeFunction(method.FunctionType, fun arg ->
                    let uri = !baseUri + method.Name
                    post.Invoke(this, [|HttpMethod.Post; uri; arg|])
                )
            )
            |> ctor

    interface IRemoteProvider with

        member this.GetService<'T>(basePath: string) =
            let basePath = normalizeBasePath(basePath)
            this.MakeRemoteProxy(typeof<'T>, ref basePath) :?> 'T

        member this.GetService<'T when 'T :> IRemoteService>() =
            let basePath = ref ""
            let proxy = this.MakeRemoteProxy(typeof<'T>, basePath) :?> 'T
            basePath := normalizeBasePath proxy.BasePath
            proxy

[<Extension>]
type ClientRemotingExtensions =

    /// Enable support for remoting in ElmishProgramComponent.
    [<Extension>]
    static member AddRemoting(services: IServiceCollection) =
        services.TryAddSingleton<IRemoteProvider, ClientRemoteProvider>()
        services
