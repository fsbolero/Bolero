namespace MiniBlazor.Server

open System
open System.IO
open System.Runtime.CompilerServices
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open MiniBlazor

type RemotingHandler(basePath: PathString, ty: System.Type, handler: obj) =

    let makeHandler (method: RemoteMethodDefinition) =
        let decoder = Json.GetDecoder method.ArgumentType
        let encoder = Json.GetEncoder method.ReturnType
        let meth = ty.GetProperty(method.Name).GetGetMethod().Invoke(handler, [||])
        let callMeth = method.FunctionType.GetMethod("Invoke")
        let output = typeof<RemotingHandler>.GetMethod("Output").MakeGenericMethod(method.ReturnType)
        fun (ctx: HttpContext) ->
            let arg =
                using (new StreamReader(ctx.Request.Body)) Json.Raw.Read
                |> decoder
            let res = callMeth.Invoke(meth, [|arg|])
            output.Invoke(null, [|ctx; encoder; res|]) :?> Task

    let methods =
        match RemotingExtensions.ExtractRemoteMethods ty with
        | Error errors ->
            raise <| AggregateException(
                "Cannot create remoting handler for type " + ty.FullName,
                [| for e in errors -> exn e |])
        | Ok methods ->
            dict [for m in methods -> m.Name, makeHandler m]

    static member Output<'Out>(ctx: HttpContext, encoder: Json.Encoder<obj>, a: Async<'Out>) : Task =
        async {
            let! x = a
            let v = encoder x
            use writer = new StreamWriter(ctx.Response.Body)
            Json.Raw.Write writer v
        }
        |> Async.StartAsTask
        :> _

    member this.TryHandle(ctx: HttpContext) : option<Task> =
        if ctx.Request.Method = "POST" && ctx.Request.Path.StartsWithSegments(basePath) then
            let reqPath = ctx.Request.Path.ToString()
            let basePath = basePath.ToString()
            let methodName = reqPath.Substring(basePath.Length).TrimStart('/')
            match methods.TryGetValue(methodName) with
            | true, handle -> Some(handle ctx)
            | false, _ -> None
        else
            None

[<Extension>]
type RemotingExtensions =

    [<Extension>]
    static member AddRemoting<'T when 'T : not struct>(this: IServiceCollection, basePath: PathString, handler: 'T) =
        this.AddSingleton<RemotingHandler>(RemotingHandler(basePath, typeof<'T>, handler))

    [<Extension>]
    static member AddRemoting<'T when 'T : not struct>(this: IServiceCollection, basePath: string, handler: 'T) =
        this.AddRemoting(PathString.op_Implicit basePath, handler)

    [<Extension>]
    static member UseRemoting(this: IApplicationBuilder) =
        let handlers =
            this.ApplicationServices.GetServices<RemotingHandler>()
            |> Array.ofSeq
        this.Use(fun ctx next ->
            handlers
            |> Array.tryPick (fun h -> h.TryHandle(ctx))
            |> Option.defaultWith next.Invoke
        )
