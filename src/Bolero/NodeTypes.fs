namespace Bolero

open Microsoft.AspNetCore.Components.Rendering

/// <summary>
/// HTML attribute or Blazor component parameter.
/// Use <see cref="T:Bolero.Html.attr" /> or <see cref="M:Bolero.Html.op_EqualsGreater" /> to create attributes.
/// </summary>
/// <category>HTML</category>
type Attr = delegate of obj * RenderTreeBuilder * int -> int

/// <summary>An HTML fragment.</summary>
/// <category>HTML</category>
type Node = delegate of obj * RenderTreeBuilder * int -> int

#if IS_DESIGNTIME
type Component() =
    inherit Microsoft.AspNetCore.Components.ComponentBase()
    member _.CssScope = ""
#endif
