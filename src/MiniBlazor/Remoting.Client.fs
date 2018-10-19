namespace MiniBlazor

open System.Net.Http
open System.Runtime.CompilerServices
open System.Text
open FSharp.Reflection
open System.IO

[<Extension>]
type RemotingExtensions =

    static member Send(client: HttpClient, method: HttpMethod, requestUri: string, content: obj) =
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
        |> client.SendAsync
        |> Async.AwaitTask

    static member SendAndParse<'T>(client, method, requestUri, content) = async {
        let! resp = RemotingExtensions.Send(client, method, requestUri, content)
        let! respBody = resp.Content.ReadAsStreamAsync() |> Async.AwaitTask
        use reader = new StreamReader(respBody)
        return Json.Read<'T> reader
    }

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
                typeof<RemotingExtensions>.GetMethod("SendAndParse")
                    .MakeGenericMethod([|resValueTy|])

            let uri = baseUri + field.Name
            FSharpValue.MakeFunction(field.PropertyType, fun arg ->
                post.Invoke(null, [|this.Http; HttpMethod.Post; uri; arg|])
            )
        )
        |> ctor
        :?> 'T
