# Changelog

## 0.17

* [#202](https://github.com/fsbolero/Bolero/issues/202) Add the ability to generate static HTML content using Bolero.Html functions.

    For example, a simple page may look like this:

    ```fsharp
    let index = doctypeHtml [] [
        head [] [
            title [] [text "Hello, world!"]
        ]
        body [] [
            div [attr.id "main"] [
                rootComp<Client.MyApp>
            ]
            boleroScript
        ]
    ]
    ```

    In this sample, the call to `rootComp` inserts the Bolero (or Blazor) component `Client.MyApp` in the page.
    The call to `boleroScript` inserts the script tag required by Blazor.

    These tags use the configuration passed to `AddBoleroHost` to determine whether the component is server-side or WebAssembly, and whether it is prerendered or not.

* [#216](https://github.com/fsbolero/Bolero/issues/216) Add helpers to create [virtualized components](https://docs.microsoft.com/en-us/aspnet/core/blazor/components/virtualization?view=aspnetcore-5.0).
    This is a Blazor feature that allows rendering only the visible items in a collection.

    ```fsharp
    // Display a virtualized list of 100 items.
    let items = [1..100]

    virtualize.comp [] items <| fun item ->
        div [] [textf "%i" item]
    ```

    The helpers provide all the features of Blazor's `Virtualize` component, such as loading from a function rather than a collection, and placeholders while items are loading:

    ```fsharp
    // Display a virtualized list of items retrieved from a remote function.

    let getItems (r: ItemsProviderRequest) : ValueTask<ItemProviderResult> =
        async {
            let! items, totalCount = remote.GetItems(r.StartIndex, r.Count)
            return ItemsProviderResult(items, totalCount)
        }
        |> Async.StartAsTask
        |> ValueTask

    // Displayed while an item is being loaded.
    let placeholder = div [attr.classes ["my-placeholder"]] []

    virtualize.compProvider
        [ virtualize.placeholder (fun _ -> placeholder) ]
        getItems
        <| fun item ->
            div [] [textf "%i" item]
    ```

* [#205](https://github.com/fsbolero/Bolero/issues/205) **Breaking change**: the `Value` property of of `Ref<'T>` and `HtmlRef` now has type `'T option` rather than `'T`.
    It is `None` if the reference hasn't been set using `attr.ref`.

* [#214](https://github.com/fsbolero/Bolero/issues/214) Fix stripping F# metadata from assemblies when building in non-trimmed mode.

## 0.16

* Upgrade from .NET Core 3.1 to .NET 5.

## 0.15

* [#56](https://github.com/fsbolero/bolero/issues/56): Update to Elmish 3.0. Also update the `Cmd` module to match Elmish 3's API, adding submodules `Cmd.OfAuthorized` and `Cmd.OfJS`.

* [#144](https://github.com/fsbolero/bolero/issues/144): When a router is enabled and the user clicks a link that points to a URI not handled by the router, do navigate to this URI.

* [#163](https://github.com/fsbolero/bolero/issues/163) Rework the HTML element reference API and add Blazor component reference:
    * `ElementRefBinder` renamed to `HtmlRef` (old name still available but obsolete)
    * `attr.bindRef` renamed to `attr.ref` (old name still available but obsolete)
    * `attr.ref` taking a function removed
    * `ref.Ref` renamed to `ref.Value`

    ```fsharp
    let theDiv = HtmlRef()    // Used to be: let theDiv = ElementRefBinder()

    div [
        attr.ref theDiv    // Used to be: attr.bindRef theDiv
        on.click (fun _ -> doSomethingWith theDiv.Value)    // Used to be: doSomethingWith theDiv.Ref
    ] []
    ```

    * Added `Ref<'Component>` which provides the same capability for Blazor components, using the same `attr.ref`:

    ```fsharp
    let theComp = Ref<MyComponent>()

    comp<MyComponent> [
        attr.ref theComp
        on.click (fun _ -> doSomethingWith theComp.Value)
    ] []
    ```

* [#166](https://github.com/fsbolero/bolero/issues/166): Ensure that Elmish subscriptions and init commands are not run during server-side prerender.

* [#168](https://github.com/fsbolero/bolero/issues/168): Move the module `Bolero.Html` to a separate assembly and make all of its functions inline, in order to reduce the downloaded binary size.

* [#174](https://github.com/fsbolero/bolero/issues/174): Change ShouldRender to invoke override instead of base implementation (thanks @dougquidd!)

* [#175](https://github.com/fsbolero/bolero/issues/175): Do not render Ref until after Child Content (thanks @dougquidd!)

## 0.14

* [#135](https://github.com/fsbolero/bolero/issues/135) Inferred router performs URL encoding/decoding on `string`-typed parameters.

* [#151](https://github.com/fsbolero/bolero/issues/151) Accept either a relative or absolute path in custom router's `getRoute`.

* [#155](https://github.com/fsbolero/bolero/issues/155) Add function `fragment` to create a Bolero `Node` from a Blazor `RenderFragment`.
    
* [#159](https://github.com/fsbolero/bolero/issues/159) **Breaking change**: Remove the module `Bolero.Json`, and use System.Text.Json together with [FSharp.SystemTextJson](https://github.com/Tarmil/FSharp.SystemTextJson) instead for remoting.

    Remoting serialization can be customized by passing an additional argument `configureSerialization: JsonSerializerOptions -> unit` to `services.AddRemoting()` in both the server-side and client-side startup functions.

## 0.13

* Update dependencies to Blazor 3.2.0.

* Add Elmish commands for JavaScript interop:
    ```fsharp
    Cmd.ofJS : IJSRuntime -> string -> obj[] -> ('res -> 'msg) -> (exn -> 'msg) -> Cmd<'msg>
    Cmd.performJS : IJSRuntime -> string -> obj[] -> ('res -> 'msg) -> Cmd<'msg>
    ```

* #127 Add `lazyComp*By` family functions based on a key function (as opposed to `lazyComp*With`'s equality function):
    ```fsharp
    lazyCompBy
         : ('model -> 'key)
        -> ('model -> Node)
        -> 'model -> Node
        when 'key : equality
    lazyComp2By
         : ('model -> 'key)
        -> ('model -> Dispatch<'msg> -> Node)
        -> 'model -> Dispatch<'msg> -> Node
        when 'key : equality
    lazyComp3By
         : ('model1 * 'model2 -> 'key)
        -> ('model1 -> 'model2 -> Dispatch<'msg> -> Node)
        -> 'model1 -> 'model2 -> Dispatch<'msg> -> Node
        when 'key : equality
    ```

* #142 Add functions to create Blazor component attributes of certain types for which `=>` is not sufficient:
    * For parameters of type `EventCallback<'T>`:
        ```fsharp
        attr.callback : string -> ('T -> unit) -> Attr
        attr.async.callback : string -> ('T -> Async<unit>) -> Attr
        attr.task.callback : string -> ('T -> Task) -> Attr
        ```
    * For parameters of type `RenderFragment`:
        ```fsharp
        attr.fragment : string -> Node -> Attr
        ```
    * For parameters of type `RenderFragment<'T>`:
        ```fsharp
        attr.fragmentWith : string -> ('T -> Node) -> Attr
        ```

* #141 Add injectable `Bolero.Server.RazorHost.IBoleroHostConfig` to provide configuration for the server-side Razor host. This is used within the Razor page by calling the extension methods on `IHtmlHelper`:
    ```fsharp
    member RenderComponentAsync<'T when 'T :> IComponent> : IBoleroHostConfig -> Task<IHtmlContent>
    member RenderBoleroScript : IBoleroHostConfig -> IHtmlContent
    ```
    and injected using the extension method on `IServiceCollection`:
    ```fsharp
    member AddBoleroHost : ?server: bool * ?prerendered: bool * ?devToggle: bool -> IServiceCollection
    ```
    
## 0.12

* #119: Correctly apply model changes to inputs using `bind.*`
* Upgrade to Blazor 3.2-preview2


## 0.11

* #95 Add `on.async.*` and `on.task.*` event handlers that use callbacks returning `Async<unit>` and `Task`, respectively.

* #86 Add `attr.aria` to create ARIA accessibility attributes.

* #97 Add `on.preventDefault` and `on.stopPropagation` to prevent the default behavior of an event and to stop its propagation to parent elements, respectively.

* #102 **BREAKING CHANGE**: The API for binders have been changed. The `bind` module now contains submodules `bind.input` and `bind.change` which in turn contain functions for the type of value being bound: `string`, `int`, `int64`, `float`, `float32`, `decimal`, `dateTime` and `dateTimeOffset`. Additionally, a module `bind.withCulture` contains the same submodules with functions taking an additional `CultureInfo` as argument to specify the culture to use to parse the value.

* #103: Optimization: use `Async.StartImmediateAsTask` rather than `Async.StartAsTask` internally.

* #105: New functions `lazyComp`, `lazyCompWith`, `lazyComp2`, `lazyComp2With`, `lazyComp3` and `lazyComp3With` allow creating components whose view is only updated when the model is actually changed. For users familiar with Fable, they are close equivalents to its `lazyView` family of functions.

* #106: `ProgramComponent.Dispatch` is now public, and can be used for scenarios where `Program.withSubscription` is insufficient.

## 0.10

* #82 Calls to `attr.classes` and ``` attr.``class`` ``` are now combined into a single `class` attribute.

    ```fsharp
    div [attr.classes ["a"; "b"]; attr.``class`` "c"; attr.classes ["d"]] []
    ```

    becomes:

    ```html
    <div class="a b c d"></div>
    ```

* #87 `ecomp` now takes an additional list of attributes as first argument.

* #89 `ProgramComponent` now has the same method `ShouldRender : 'model * 'model -> bool` that `ElmishComponent` has. The full program is not re-rendered if this returns false after an update. Thanks @laenas!

* A new type alias `Program<'model, 'msg>` represents the exact type of Elmish programs used by Bolero. It corresponds to `Program<ProgramComponent<'model, 'msg>, 'model, 'msg, Node>`.

## 0.9

* Updated .NET Core dependency to version 3.0 RTM and Blazor to 3.0-preview9.

* #78, #79: Add [PageModels](https://fsbolero.io/docs/Routing#page-models). PageModels allow adding a model specific to a page. The new APIs are:

    ```fsharp
    namespace Bolero

    type PageModel<'T> = { Model : 'T }

    module Router =

        val inferWithModel
            : makeMessage: ('page -> 'msg)
           -> getEndPoint: ('model -> 'page)
           -> defaultPageModel: ('page -> unit)
           -> Router<'page, 'model, 'msg>

        val noModel : PageModel<'T>

        val definePageModel : PageModel<'T> -> 'T -> unit
    ```
    
## 0.8

* Updated Blazor and .NET Core dependencies to version 3.0-preview8, with associated API changes.

* #61 Add `attr.key` to uniquely identify elements in a list to help the renderer. [See the corresponding @key attribute in the Blazor documentation.](https://docs.microsoft.com/en-us/aspnet/core/blazor/components?view=aspnetcore-3.0#use-key-to-control-the-preservation-of-elements-and-components)

* #70 Correctly provide the `HttpContext` to remote functions when running in server mode.

* #71 Add new exception `RemoteException of HttpResponseMessage`, which is thrown on the client side when the response code is neither 200 OK (which succeeds) nor 401 Unauthorized (which throws `RemoteUnauthorizedException`).

* #73 **Breaking** Removed the server-side module `Bolero.Remoting.Server.Remote` with its functions `withHttpContext`, `authorize` and `authorizeWith`.

    Instead, a new type `IRemoteContext` is provided via dependency injection:

    ```fsharp
    type IRemoteContext =
        inherit IHttpContextAccessor // member HttpContext : HttpContext with get, set
        member Authorize : ('req -> Async<'resp>) -> ('req -> Async<'resp>)
        member AuthorizeWith : seq<IAuthorizeData> -> ('req -> Async<'resp>) -> ('req -> Async<'resp>)
    ```

    There are also new overloads on `IServiceCollection.AddRemoting` that take `IRemoteContext -> 'Handler` as argument, so that remote handlers that use authorization don't need to switch to using DI.
    
## 0.7

* Bolero.HotReload, the HTML template hot reload library, had been blocked from upgrade by a dependency; it is now available again for the latest Bolero.

* `Cmd.ofRemote` and its cousin `Cmd.performRemote` have been deprecated. We felt that these function names were misleading, because they are only useful when calling authorized remote functions. Remote functions without user authorization can be simply called with `Cmd.ofAsync` or `Cmd.performAsync`.
    The new functions `Cmd.ofAuthorized` or `Cmd.performAuthorized` should now be used instead. They are identical to the previous `*Remote`, except that instead of passing the response as a custom type `RemoteResponse<'T>`, they use a simple `option<'T>`, which is `Some` on success and `None` in case of authorization failure.
