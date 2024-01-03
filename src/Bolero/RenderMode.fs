namespace Bolero
#if NET8_0_OR_GREATER

open System.Runtime.InteropServices
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Web

type BoleroRenderMode =
    /// <summary>
    /// Render the component and run its interactivity on the server side.
    /// </summary>
    | Server = 1
    /// <summary>
    /// Render the component and run its interactivity on the client side.
    /// </summary>
    | WebAssembly = 2
    /// <summary>
    /// Automatically decide where to render the component and run its interactivity.
    /// </summary>
    | Auto = 3

/// <summary>
/// Define how a component is rendered in interactive render mode.
/// </summary>
type BoleroRenderModeAttribute
    /// <summary>
    /// Define how a component is rendered in interactive render mode.
    /// </summary>
    /// <param name="mode">The render mode.</param>
    /// <param name="prerender">Whether to prerender the component in the HTML response.</param>
    (mode, [<Optional; DefaultParameterValue true>] prerender: bool) =
    inherit RenderModeAttribute()

    /// <inheritdoc />
    override val Mode : IComponentRenderMode =
        match mode with
        | BoleroRenderMode.Server -> InteractiveServerRenderMode(prerender)
        | BoleroRenderMode.WebAssembly -> InteractiveWebAssemblyRenderMode(prerender)
        | BoleroRenderMode.Auto -> InteractiveAutoRenderMode(prerender)
        | _ -> failwith $"Invalid InteractiveRenderMode: {mode}"

#endif
