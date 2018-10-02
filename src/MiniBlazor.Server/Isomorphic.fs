namespace rec MiniBlazor

open System
open System.IO
open System.Net
open System.Reflection
open System.Runtime.CompilerServices
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Blazor.Builder
open Microsoft.Extensions.DependencyInjection
open Fizzler.Systems.HtmlAgilityPack
open FSharp.Quotations
open FSharp.Quotations.Patterns

type IIsomorphicApp =
    abstract Run : unit -> unit
    abstract BlazorOptions : BlazorOptions
    abstract Respond : Http.HttpContext * serverSide: bool -> Task

type IsomorphicApp<'Message, 'Model> =
    {
        app: MiniBlazor.App.App<'Message, 'Model>
        assembly: Assembly
        file: string
        selector: string
    }

    static member Of([<ReflectedDefinition true>] app: Expr<MiniBlazor.App.App<'Message, 'Model>>) =
        match app with
        | WithValue(app, _, PropertyGet(None, p, _)) ->
            IsomorphicApp<'Message, 'Model>
                .Of(unbox app, p.DeclaringType.Assembly)
        | _ ->
            failwith "You must pass a module top-level value as app, or call IsomorphicApp.Of(app, assembly)."

    static member Of(app, assembly) : IsomorphicApp<'Message, 'Model> =
        {
            app = app
            assembly = assembly
            file = "index.html"
            selector = "#main"
        }

    interface IIsomorphicApp with

        member this.Run() =
            MiniBlazor.App.Run this.selector this.app

        member this.BlazorOptions =
            BlazorOptions(ClientAssemblyPath = this.assembly.Location)

        member this.Respond(ctx, serverSide) =
            IsomorphicRunner.Respond this ctx serverSide

module IsomorphicRunner =
    open HtmlAgilityPack
    open MiniBlazor.Html

    let rec RenderNode (parent: HtmlNode) = function
        | Text text ->
            parent.OwnerDocument.CreateTextNode(text)
            |> parent.AppendChild
            |> ignore
        | Empty -> ()
        | Concat nodes ->
            Seq.iter (RenderNode parent) nodes
        | Elt(name, attrs, _events, children) ->
            let elt = parent.OwnerDocument.CreateElement(name)
            for KeyValue(key, value) in attrs do
                elt.SetAttributeValue(key, value) |> ignore
            Seq.iter (RenderNode elt) children
            parent.AppendChild(elt) |> ignore
        | KeyedFragment nodes ->
            Seq.iter (snd >> RenderNode parent) nodes

    let Run (app: IsomorphicApp<'Message, 'Model>) (path: string) (resp: Http.HttpResponse) serverSide =
        let doc = HtmlAgilityPack.HtmlDocument()
        doc.Load(path)
        let root = doc.DocumentNode
        for n in root.QuerySelectorAll("script") do
            let s = n.GetAttributeValue("src", "")
            if serverSide && s.Contains("blazor.webassembly.js") then
                n.SetAttributeValue("src",
                    s.Replace("blazor.webassembly.js", "blazor.server.js"))
                |> ignore
            elif not serverSide && s.Contains("blazor.server.js") then
                n.SetAttributeValue("src",
                    s.Replace("blazor.server.js", "blazor.webassembly.js"))
                |> ignore
        match root.QuerySelector(app.selector) with
        | null ->
            failwithf "Cannot find selector '%s' in file: %s" app.selector app.file
        | container ->
            app.app.Render app.app.Init
            |> RenderNode container
            container.SetAttributeValue("data-miniblazor-hydrate", "true") |> ignore
            resp.ContentType <- "text/html"
            doc.Save(resp.Body)

    // Copied from BlazorConfig.cs
    let GetWebRootPath (assembly: Assembly) =
        let configFilePath = Path.ChangeExtension(assembly.Location, ".blazor.config")
        let configLines = File.ReadAllLines(configFilePath)
        let sourceMSBuildPath = configLines.[0]
        let sourceMSBuildDir = Path.GetDirectoryName(sourceMSBuildPath)
        Path.Combine(sourceMSBuildDir, "wwwroot")

    let Respond (isoApp: IsomorphicApp<_, _>) (ctx: Http.HttpContext) serverSide =
        let path =
            if Path.IsPathRooted(isoApp.file) then
                isoApp.file
            else
                let root = GetWebRootPath isoApp.assembly
                Path.Combine(root, isoApp.file)
        Task.Factory.StartNew(fun () -> Run isoApp path ctx.Response serverSide)

type MiniBlazorStartup(isoApp: IIsomorphicApp) =

    member this.App = isoApp

    member this.Configure(app: IBlazorApplicationBuilder) =
        this.App.Run()


[<Extension>]
type IApplicationBuilderExtensions =

    [<Extension>]
    static member internal UseIsomorphicImpl
        (
            this: IApplicationBuilder,
            path: string,
            isoApp: IIsomorphicApp,
            serverSide: bool
        ) =
        this.Use(fun ctx next ->
            if ctx.Request.Path.Value = path then
                isoApp.Respond(ctx, serverSide)
            else
                next.Invoke())

    [<Extension>]
    static member UseIsomorphic
        (
            this: IApplicationBuilder,
            path: string,
            isoApp: IIsomorphicApp
        ) =
        this.UseIsomorphicImpl(path, isoApp, false)
            .UseBlazor(isoApp.BlazorOptions)

    [<Extension>]
    static member UseIsomorphic<'T when 'T :> MiniBlazorStartup and 'T : not struct>
        (
            this: IApplicationBuilder,
            path: string
        ) =
        let app = this.ApplicationServices.GetService<'T>().App
        this.UseIsomorphic(path, app)

    [<Extension>]
    static member UseServerSideIsomorphic
        (
            this: IApplicationBuilder,
            path: string,
            isoApp: IIsomorphicApp
        ) =
        this.UseIsomorphicImpl(path, isoApp, true)
            .UseServerSideBlazor(isoApp.BlazorOptions)

    [<Extension>]
    static member UseServerSideIsomorphic<'T when 'T :> MiniBlazorStartup and 'T : not struct>
        (
            this: IApplicationBuilder,
            path: string
        ) =
        let app = this.ApplicationServices.GetService<'T>().App
        this.UseServerSideIsomorphic(path, app)

[<Extension>]
type IServiceCollectionExtensions =

    [<Extension>]
    static member AddIsomorphic<'T when 'T :> MiniBlazorStartup and 'T : not struct>
        (
            this: IServiceCollection
        ) =
        this.AddSingleton<'T>()
            .AddServerSideBlazor<'T>()
