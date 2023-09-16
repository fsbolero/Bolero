namespace Microsoft.AspNetCore.Components.Web

type ClipboardEventArgs = class end
type DragEventArgs = class end
type FocusEventArgs = class end
type KeyboardEventArgs = class end
type MouseEventArgs = class end
type PointerEventArgs = class end
type ProgressEventArgs = class end
type TouchEventArgs = class end
type WheelEventArgs = class inherit MouseEventArgs end

namespace Microsoft.AspNetCore.Components.Web.Virtualization

open Microsoft.AspNetCore.Components

type Virtualize<'TItem>() =
    inherit ComponentBase()
