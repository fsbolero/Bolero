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
        if not (FSharpType.IsRecord ty) then
            failwithf "Remote type must be a record: %s" ty.FullName
        let fields = FSharpType.GetRecordFields(ty, true)
        let ctor = FSharpValue.PreComputeRecordConstructor(ty, true)
        fields
        |> Array.map (fun field ->
            let rec getArgs pty =
                if FSharpType.IsFunction pty then
                    let domain, codomain = FSharpType.GetFunctionElements(pty)
                    let restDomain, finalCodomain = getArgs codomain
                    domain :: restDomain, finalCodomain
                else
                    [], pty
            match getArgs field.PropertyType with
            | [], _ -> failwithf "Remote type field must be a function: %s.%s" ty.FullName field.Name
            | args, res ->
                if not (res.IsGenericType && res.GetGenericTypeDefinition() = typedefof<Async<_>>) then
                    failwithf "Remote function must return Async<_>: %s.%s" ty.FullName field.Name
                let post =
                    typeof<RemotingExtensions>.GetMethod("RemotePost").MakeGenericMethod([|res|])
                let uri =
                    baseUri +
                    (if baseUri.EndsWith "/" then "" else "/") +
                    field.Name
                let rec buildFunc acc = function
                    | [_argTy] ->
                        fun arg ->
                            let args =
                                arg :: acc
                                |> Array.ofList
                                |> Array.rev
                            post.Invoke(null, [|this.Http; uri; args|])
                    | _argTy :: argsTy ->
                        fun arg ->
                            box (buildFunc (arg :: acc) argsTy)
                    | [] -> failwith "Impossible"
                box (buildFunc args)
        )
        |> ctor
        :?> 'T
