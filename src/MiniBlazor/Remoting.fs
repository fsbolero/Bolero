namespace MiniBlazor

open System.Net.Http
open System.Reflection
open System.Runtime.CompilerServices
open Microsoft.AspNetCore.Blazor
open Microsoft.JSInterop
open FSharp.Reflection

[<Extension>]
type RemotingExtensions =

    [<Extension>]
    static member AsyncSendJson(this: HttpClient, method: HttpMethod, requestUri: string, content: obj) =
        this.SendJsonAsync(method, requestUri, content) |> Async.AwaitTask

    [<Extension>]
    static member AsyncSendJson<'T>(this: HttpClient, method: HttpMethod, requestUri: string, content: obj) =
        this.SendJsonAsync<'T>(method, requestUri, content) |> Async.AwaitTask

    [<Extension>]
    static member AsyncGetJson<'T>(this: HttpClient, requestUri: string) =
        this.GetJsonAsync<'T>(requestUri) |> Async.AwaitTask

    [<Extension>]
    static member AsyncPostJson(this: HttpClient, requestUri: string, content: obj) =
        this.PostJsonAsync(requestUri, content) |> Async.AwaitTask

    [<Extension>]
    static member AsyncPostJson<'T>(this: HttpClient, requestUri: string, content: obj) =
        this.PostJsonAsync<'T>(requestUri, content) |> Async.AwaitTask

    [<Extension>]
    static member AsyncPutJson(this: HttpClient, requestUri: string, content: obj) =
        this.PutJsonAsync(requestUri, content) |> Async.AwaitTask

    [<Extension>]
    static member AsyncPutJson<'T>(this: HttpClient, requestUri: string, content: obj) =
        this.PutJsonAsync<'T>(requestUri, content) |> Async.AwaitTask

    static member RemotePost<'T>(this: HttpClient, requestUri: string, content: obj) =
        this.AsyncPostJson<'T>(requestUri, content)

    [<Extension>]
    static member Remote<'T>(this: IElmishProgramComponent, baseUri: string) =
        let ty = typeof<'T>
        let baseUri = baseUri + (if baseUri.EndsWith "/" then "" else "/")
        if not (FSharpType.IsRecord ty) then
            failwithf "Remote type must be a record: %s" ty.FullName
        let fields = FSharpType.GetRecordFields(ty, true)
        let ctor = FSharpValue.PreComputeRecordConstructor(ty, true)
        fields
        |> Array.map (fun field ->
            let fnErr fmt = failwithf fmt ty.FullName field.Name

            if not (FSharpType.IsFunction field.PropertyType) then
                fnErr "Remote type field must be an F# function: %s.%s"
            let _, resTy = FSharpType.GetFunctionElements(field.PropertyType)
            if FSharpType.IsFunction resTy then
                fnErr "Remote type field must be an F# function with only one argument: %s.%s. Use a tuple if needed"
            if not (resTy.IsGenericType && resTy.GetGenericTypeDefinition() = typedefof<Async<_>>) then
                fnErr "Remote function must return Async<_>: %s.%s"

            let resValueTy = resTy.GetGenericArguments().[0]
            let post =
                typeof<RemotingExtensions>.GetMethod("RemotePost")
                    .MakeGenericMethod([|resValueTy|])

            let uri = baseUri + field.Name
            FSharpValue.MakeFunction(field.PropertyType, fun arg ->
                post.Invoke(null, [|this.Http; uri; arg|])
            )
        )
        |> ctor
        :?> 'T
