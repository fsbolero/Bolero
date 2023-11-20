namespace Microsoft.AspNetCore.Components

type ChangeEventArgs = member _.Value = obj()
type IComponent = interface end
type ComponentBase() = interface IComponent
type ElementReference = class end

namespace Microsoft.AspNetCore.Components.Rendering

open System
open Microsoft.AspNetCore.Components

type RenderTreeBuilder =
    member _.OpenElement(_: int, _: string) : unit = raise (NotImplementedException())
    member _.CloseElement() : unit = raise (NotImplementedException())
    member _.OpenComponent<'T when 'T :> IComponent>(_: int) : unit = raise (NotImplementedException())
    member _.OpenComponent(_: int, _: Type) : unit = raise (NotImplementedException())
    member _.CloseComponent() : unit = raise (NotImplementedException())
    member _.AddAttribute(_: int, _: string, _: obj) : unit = raise (NotImplementedException())
    member _.AddAttribute(_: int, _: string) : unit = raise (NotImplementedException())
    member _.AddContent(_: int, _: string) : unit = raise (NotImplementedException())
    member _.AddMarkupContent(_: int, _: string) : unit = raise (NotImplementedException())
    member _.OpenRegion(_: int) : unit = raise (NotImplementedException())
    member _.CloseRegion() : unit = raise (NotImplementedException())
    member _.AddComponentReferenceCapture(_: int, _: Action<obj>) = raise (NotImplementedException())
    member _.AddElementReferenceCapture(_: int, _: Action<ElementReference>) = raise (NotImplementedException())

namespace Microsoft.AspNetCore.Components

open Microsoft.AspNetCore.Components.Rendering

type RenderFragment = delegate of RenderTreeBuilder -> unit
type RenderFragment<'T> = delegate of 'T -> RenderFragment
