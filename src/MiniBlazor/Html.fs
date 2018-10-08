module rec MiniBlazor.Html

open Microsoft.AspNetCore.Blazor

let text str = Text str

let elt name attrs children = Node.Elt(name, attrs, children)

let empty = Empty

let concat nodes = Concat nodes

let (=>) name value = (name, box value)

let comp<'T when 'T :> Components.IComponent> attrs children =
    let rec nodeLength = function
        | Empty -> 0
        | Text _ -> 1
        | Concat nodes -> List.sumBy nodeLength nodes
        | Elt (_, attrs, children) ->
            1 + List.length attrs + List.sumBy nodeLength children
        | Component (_, i, _, _) -> i.length
    let length = 1 + List.length attrs + List.sumBy nodeLength children
    Node.Component(typeof<'T>, { length = length }, attrs, children)

let ecomp<'T, 'model, 'msg when 'T :> ElmishComponent<'model, 'msg>>
        (model: 'model) (dispatch: Elmish.Dispatch<'msg>) =
    comp<'T> ["Model" => model; "Dispatch" => dispatch] []

// BEGIN TAGS
let a (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "a" attrs children
let abbr (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "abbr" attrs children
let acronym (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "acronym" attrs children
let address (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "address" attrs children
let applet (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "applet" attrs children
let area (attrs: list<Attr>) : Node =
    elt "area" attrs []
let article (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "article" attrs children
let aside (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "aside" attrs children
let audio (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "audio" attrs children
let b (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "b" attrs children
let ``base`` (attrs: list<Attr>) : Node =
    elt "base" attrs []
let basefont (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "basefont" attrs children
let bdi (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "bdi" attrs children
let bdo (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "bdo" attrs children
let big (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "big" attrs children
let blockquote (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "blockquote" attrs children
let body (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "body" attrs children
let br (attrs: list<Attr>) : Node =
    elt "br" attrs []
let button (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "button" attrs children
let canvas (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "canvas" attrs children
let caption (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "caption" attrs children
let center (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "center" attrs children
let cite (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "cite" attrs children
let code (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "code" attrs children
let col (attrs: list<Attr>) : Node =
    elt "col" attrs []
let colgroup (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "colgroup" attrs children
let content (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "content" attrs children
let data (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "data" attrs children
let datalist (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "datalist" attrs children
let dd (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "dd" attrs children
let del (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "del" attrs children
let details (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "details" attrs children
let dfn (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "dfn" attrs children
let dialog (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "dialog" attrs children
let dir (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "dir" attrs children
let div (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "div" attrs children
let dl (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "dl" attrs children
let dt (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "dt" attrs children
let element (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "element" attrs children
let em (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "em" attrs children
let embed (attrs: list<Attr>) : Node =
    elt "embed" attrs []
let fieldset (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "fieldset" attrs children
let figcaption (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "figcaption" attrs children
let figure (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "figure" attrs children
let font (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "font" attrs children
let footer (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "footer" attrs children
let form (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "form" attrs children
let frame (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "frame" attrs children
let frameset (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "frameset" attrs children
let h1 (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "h1" attrs children
let h2 (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "h2" attrs children
let h3 (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "h3" attrs children
let h4 (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "h4" attrs children
let h5 (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "h5" attrs children
let h6 (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "h6" attrs children
let head (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "head" attrs children
let header (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "header" attrs children
let hr (attrs: list<Attr>) : Node =
    elt "hr" attrs []
let html (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "html" attrs children
let i (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "i" attrs children
let iframe (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "iframe" attrs children
let img (attrs: list<Attr>) : Node =
    elt "img" attrs []
let input (attrs: list<Attr>) : Node =
    elt "input" attrs []
let ins (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "ins" attrs children
let kbd (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "kbd" attrs children
let label (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "label" attrs children
let legend (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "legend" attrs children
let li (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "li" attrs children
let link (attrs: list<Attr>) : Node =
    elt "link" attrs []
let main (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "main" attrs children
let map (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "map" attrs children
let mark (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "mark" attrs children
let menu (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "menu" attrs children
let menuitem (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "menuitem" attrs children
let meta (attrs: list<Attr>) : Node =
    elt "meta" attrs []
let meter (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "meter" attrs children
let nav (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "nav" attrs children
let noembed (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "noembed" attrs children
let noframes (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "noframes" attrs children
let noscript (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "noscript" attrs children
let object (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "object" attrs children
let ol (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "ol" attrs children
let optgroup (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "optgroup" attrs children
let option (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "option" attrs children
let output (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "output" attrs children
let p (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "p" attrs children
let param (attrs: list<Attr>) : Node =
    elt "param" attrs []
let picture (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "picture" attrs children
let pre (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "pre" attrs children
let progress (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "progress" attrs children
let q (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "q" attrs children
let rb (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "rb" attrs children
let rp (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "rp" attrs children
let rt (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "rt" attrs children
let rtc (attrs: list<Attr>) : Node =
    elt "rtc" attrs []
let ruby (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "ruby" attrs children
let s (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "s" attrs children
let samp (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "samp" attrs children
let script (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "script" attrs children
let section (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "section" attrs children
let select (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "select" attrs children
let shadow (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "shadow" attrs children
let slot (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "slot" attrs children
let small (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "small" attrs children
let source (attrs: list<Attr>) : Node =
    elt "source" attrs []
let span (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "span" attrs children
let strike (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "strike" attrs children
let strong (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "strong" attrs children
let style (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "style" attrs children
let sub (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "sub" attrs children
let summary (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "summary" attrs children
let sup (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "sup" attrs children
let svg (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "svg" attrs children
let table (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "table" attrs children
let tbody (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "tbody" attrs children
let td (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "td" attrs children
let template (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "template" attrs children
let textarea (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "textarea" attrs children
let tfoot (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "tfoot" attrs children
let th (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "th" attrs children
let thead (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "thead" attrs children
let time (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "time" attrs children
let title (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "title" attrs children
let tr (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "tr" attrs children
let track (attrs: list<Attr>) : Node =
    elt "track" attrs []
let tt (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "tt" attrs children
let u (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "u" attrs children
let ul (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "ul" attrs children
let var (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "var" attrs children
let video (attrs: list<Attr>) (children: list<Node>) : Node =
    elt "video" attrs children
let wbr (attrs: list<Attr>) : Node =
    elt "wbr" attrs []
// END TAGS

module attr =
// BEGIN ATTRS
    let accept (v: obj) : Attr = "accept" => v
    let acceptCharset (v: obj) : Attr = "accept-charset" => v
    let accesskey (v: obj) : Attr = "accesskey" => v
    let action (v: obj) : Attr = "action" => v
    let align (v: obj) : Attr = "align" => v
    let allow (v: obj) : Attr = "allow" => v
    let alt (v: obj) : Attr = "alt" => v
    let async (v: obj) : Attr = "async" => v
    let autocapitalize (v: obj) : Attr = "autocapitalize" => v
    let autocomplete (v: obj) : Attr = "autocomplete" => v
    let autofocus (v: obj) : Attr = "autofocus" => v
    let autoplay (v: obj) : Attr = "autoplay" => v
    let bgcolor (v: obj) : Attr = "bgcolor" => v
    let border (v: obj) : Attr = "border" => v
    let buffered (v: obj) : Attr = "buffered" => v
    let challenge (v: obj) : Attr = "challenge" => v
    let charset (v: obj) : Attr = "charset" => v
    let ``checked`` (v: obj) : Attr = "checked" => v
    let cite (v: obj) : Attr = "cite" => v
    let ``class`` (v: obj) : Attr = "class" => v
    let code (v: obj) : Attr = "code" => v
    let codebase (v: obj) : Attr = "codebase" => v
    let color (v: obj) : Attr = "color" => v
    let cols (v: obj) : Attr = "cols" => v
    let colspan (v: obj) : Attr = "colspan" => v
    let content (v: obj) : Attr = "content" => v
    let contenteditable (v: obj) : Attr = "contenteditable" => v
    let contextmenu (v: obj) : Attr = "contextmenu" => v
    let controls (v: obj) : Attr = "controls" => v
    let coords (v: obj) : Attr = "coords" => v
    let crossorigin (v: obj) : Attr = "crossorigin" => v
    let csp (v: obj) : Attr = "csp" => v
    let data (v: obj) : Attr = "data" => v
    let datetime (v: obj) : Attr = "datetime" => v
    let decoding (v: obj) : Attr = "decoding" => v
    let ``default`` (v: obj) : Attr = "default" => v
    let defer (v: obj) : Attr = "defer" => v
    let dir (v: obj) : Attr = "dir" => v
    let dirname (v: obj) : Attr = "dirname" => v
    let disabled (v: obj) : Attr = "disabled" => v
    let download (v: obj) : Attr = "download" => v
    let draggable (v: obj) : Attr = "draggable" => v
    let dropzone (v: obj) : Attr = "dropzone" => v
    let enctype (v: obj) : Attr = "enctype" => v
    let ``for`` (v: obj) : Attr = "for" => v
    let form (v: obj) : Attr = "form" => v
    let formaction (v: obj) : Attr = "formaction" => v
    let headers (v: obj) : Attr = "headers" => v
    let height (v: obj) : Attr = "height" => v
    let hidden (v: obj) : Attr = "hidden" => v
    let high (v: obj) : Attr = "high" => v
    let href (v: obj) : Attr = "href" => v
    let hreflang (v: obj) : Attr = "hreflang" => v
    let httpEquiv (v: obj) : Attr = "http-equiv" => v
    let icon (v: obj) : Attr = "icon" => v
    let id (v: obj) : Attr = "id" => v
    let importance (v: obj) : Attr = "importance" => v
    let integrity (v: obj) : Attr = "integrity" => v
    let ismap (v: obj) : Attr = "ismap" => v
    let itemprop (v: obj) : Attr = "itemprop" => v
    let keytype (v: obj) : Attr = "keytype" => v
    let kind (v: obj) : Attr = "kind" => v
    let label (v: obj) : Attr = "label" => v
    let lang (v: obj) : Attr = "lang" => v
    let language (v: obj) : Attr = "language" => v
    let lazyload (v: obj) : Attr = "lazyload" => v
    let list (v: obj) : Attr = "list" => v
    let loop (v: obj) : Attr = "loop" => v
    let low (v: obj) : Attr = "low" => v
    let manifest (v: obj) : Attr = "manifest" => v
    let max (v: obj) : Attr = "max" => v
    let maxlength (v: obj) : Attr = "maxlength" => v
    let media (v: obj) : Attr = "media" => v
    let method (v: obj) : Attr = "method" => v
    let min (v: obj) : Attr = "min" => v
    let minlength (v: obj) : Attr = "minlength" => v
    let multiple (v: obj) : Attr = "multiple" => v
    let muted (v: obj) : Attr = "muted" => v
    let name (v: obj) : Attr = "name" => v
    let novalidate (v: obj) : Attr = "novalidate" => v
    let ``open`` (v: obj) : Attr = "open" => v
    let optimum (v: obj) : Attr = "optimum" => v
    let pattern (v: obj) : Attr = "pattern" => v
    let ping (v: obj) : Attr = "ping" => v
    let placeholder (v: obj) : Attr = "placeholder" => v
    let poster (v: obj) : Attr = "poster" => v
    let preload (v: obj) : Attr = "preload" => v
    let readonly (v: obj) : Attr = "readonly" => v
    let rel (v: obj) : Attr = "rel" => v
    let required (v: obj) : Attr = "required" => v
    let reversed (v: obj) : Attr = "reversed" => v
    let rows (v: obj) : Attr = "rows" => v
    let rowspan (v: obj) : Attr = "rowspan" => v
    let sandbox (v: obj) : Attr = "sandbox" => v
    let scope (v: obj) : Attr = "scope" => v
    let selected (v: obj) : Attr = "selected" => v
    let shape (v: obj) : Attr = "shape" => v
    let size (v: obj) : Attr = "size" => v
    let sizes (v: obj) : Attr = "sizes" => v
    let slot (v: obj) : Attr = "slot" => v
    let span (v: obj) : Attr = "span" => v
    let spellcheck (v: obj) : Attr = "spellcheck" => v
    let src (v: obj) : Attr = "src" => v
    let srcdoc (v: obj) : Attr = "srcdoc" => v
    let srclang (v: obj) : Attr = "srclang" => v
    let srcset (v: obj) : Attr = "srcset" => v
    let start (v: obj) : Attr = "start" => v
    let step (v: obj) : Attr = "step" => v
    let style (v: obj) : Attr = "style" => v
    let summary (v: obj) : Attr = "summary" => v
    let tabindex (v: obj) : Attr = "tabindex" => v
    let target (v: obj) : Attr = "target" => v
    let title (v: obj) : Attr = "title" => v
    let translate (v: obj) : Attr = "translate" => v
    let ``type`` (v: obj) : Attr = "type" => v
    let usemap (v: obj) : Attr = "usemap" => v
    let value (v: obj) : Attr = "value" => v
    let width (v: obj) : Attr = "width" => v
    let wrap (v: obj) : Attr = "wrap" => v
// END ATTRS

module on =
    open Microsoft.AspNetCore.Blazor.Components

    let event<'T when 'T :> UIEventArgs> event (callback: 'T -> unit) =
        "on" + event => BindMethods.GetEventHandlerValue callback

// BEGIN EVENTS
    let focus (callback: UIFocusEventArgs -> unit) : Attr =
        "onfocus" => BindMethods.GetEventHandlerValue callback
    let blur (callback: UIFocusEventArgs -> unit) : Attr =
        "onblur" => BindMethods.GetEventHandlerValue callback
    let focusin (callback: UIFocusEventArgs -> unit) : Attr =
        "onfocusin" => BindMethods.GetEventHandlerValue callback
    let focusout (callback: UIFocusEventArgs -> unit) : Attr =
        "onfocusout" => BindMethods.GetEventHandlerValue callback
    let mouseover (callback: UIMouseEventArgs -> unit) : Attr =
        "onmouseover" => BindMethods.GetEventHandlerValue callback
    let mouseout (callback: UIMouseEventArgs -> unit) : Attr =
        "onmouseout" => BindMethods.GetEventHandlerValue callback
    let mousemove (callback: UIMouseEventArgs -> unit) : Attr =
        "onmousemove" => BindMethods.GetEventHandlerValue callback
    let mousedown (callback: UIMouseEventArgs -> unit) : Attr =
        "onmousedown" => BindMethods.GetEventHandlerValue callback
    let mouseup (callback: UIMouseEventArgs -> unit) : Attr =
        "onmouseup" => BindMethods.GetEventHandlerValue callback
    let click (callback: UIMouseEventArgs -> unit) : Attr =
        "onclick" => BindMethods.GetEventHandlerValue callback
    let dblclick (callback: UIMouseEventArgs -> unit) : Attr =
        "ondblclick" => BindMethods.GetEventHandlerValue callback
    let wheel (callback: UIMouseEventArgs -> unit) : Attr =
        "onwheel" => BindMethods.GetEventHandlerValue callback
    let mousewheel (callback: UIMouseEventArgs -> unit) : Attr =
        "onmousewheel" => BindMethods.GetEventHandlerValue callback
    let contextmenu (callback: UIMouseEventArgs -> unit) : Attr =
        "oncontextmenu" => BindMethods.GetEventHandlerValue callback
    let drag (callback: UIDragEventArgs -> unit) : Attr =
        "ondrag" => BindMethods.GetEventHandlerValue callback
    let dragend (callback: UIDragEventArgs -> unit) : Attr =
        "ondragend" => BindMethods.GetEventHandlerValue callback
    let dragenter (callback: UIDragEventArgs -> unit) : Attr =
        "ondragenter" => BindMethods.GetEventHandlerValue callback
    let dragleave (callback: UIDragEventArgs -> unit) : Attr =
        "ondragleave" => BindMethods.GetEventHandlerValue callback
    let dragover (callback: UIDragEventArgs -> unit) : Attr =
        "ondragover" => BindMethods.GetEventHandlerValue callback
    let dragstart (callback: UIDragEventArgs -> unit) : Attr =
        "ondragstart" => BindMethods.GetEventHandlerValue callback
    let drop (callback: UIDragEventArgs -> unit) : Attr =
        "ondrop" => BindMethods.GetEventHandlerValue callback
    let keydown (callback: UIKeyboardEventArgs -> unit) : Attr =
        "onkeydown" => BindMethods.GetEventHandlerValue callback
    let keyup (callback: UIKeyboardEventArgs -> unit) : Attr =
        "onkeyup" => BindMethods.GetEventHandlerValue callback
    let keypress (callback: UIKeyboardEventArgs -> unit) : Attr =
        "onkeypress" => BindMethods.GetEventHandlerValue callback
    let change (callback: UIChangeEventArgs -> unit) : Attr =
        "onchange" => BindMethods.GetEventHandlerValue callback
    let input (callback: UIEventArgs -> unit) : Attr =
        "oninput" => BindMethods.GetEventHandlerValue callback
    let invalid (callback: UIEventArgs -> unit) : Attr =
        "oninvalid" => BindMethods.GetEventHandlerValue callback
    let reset (callback: UIEventArgs -> unit) : Attr =
        "onreset" => BindMethods.GetEventHandlerValue callback
    let select (callback: UIEventArgs -> unit) : Attr =
        "onselect" => BindMethods.GetEventHandlerValue callback
    let selectstart (callback: UIEventArgs -> unit) : Attr =
        "onselectstart" => BindMethods.GetEventHandlerValue callback
    let selectionchange (callback: UIEventArgs -> unit) : Attr =
        "onselectionchange" => BindMethods.GetEventHandlerValue callback
    let submit (callback: UIEventArgs -> unit) : Attr =
        "onsubmit" => BindMethods.GetEventHandlerValue callback
    let beforecopy (callback: UIEventArgs -> unit) : Attr =
        "onbeforecopy" => BindMethods.GetEventHandlerValue callback
    let beforecut (callback: UIEventArgs -> unit) : Attr =
        "onbeforecut" => BindMethods.GetEventHandlerValue callback
    let beforepaste (callback: UIEventArgs -> unit) : Attr =
        "onbeforepaste" => BindMethods.GetEventHandlerValue callback
    let copy (callback: UIClipboardEventArgs -> unit) : Attr =
        "oncopy" => BindMethods.GetEventHandlerValue callback
    let cut (callback: UIClipboardEventArgs -> unit) : Attr =
        "oncut" => BindMethods.GetEventHandlerValue callback
    let paste (callback: UIClipboardEventArgs -> unit) : Attr =
        "onpaste" => BindMethods.GetEventHandlerValue callback
    let touchcancel (callback: UITouchEventArgs -> unit) : Attr =
        "ontouchcancel" => BindMethods.GetEventHandlerValue callback
    let touchend (callback: UITouchEventArgs -> unit) : Attr =
        "ontouchend" => BindMethods.GetEventHandlerValue callback
    let touchmove (callback: UITouchEventArgs -> unit) : Attr =
        "ontouchmove" => BindMethods.GetEventHandlerValue callback
    let touchstart (callback: UITouchEventArgs -> unit) : Attr =
        "ontouchstart" => BindMethods.GetEventHandlerValue callback
    let touchenter (callback: UITouchEventArgs -> unit) : Attr =
        "ontouchenter" => BindMethods.GetEventHandlerValue callback
    let touchleave (callback: UITouchEventArgs -> unit) : Attr =
        "ontouchleave" => BindMethods.GetEventHandlerValue callback
    let pointercapture (callback: UIPointerEventArgs -> unit) : Attr =
        "onpointercapture" => BindMethods.GetEventHandlerValue callback
    let lostpointercapture (callback: UIPointerEventArgs -> unit) : Attr =
        "onlostpointercapture" => BindMethods.GetEventHandlerValue callback
    let pointercancel (callback: UIPointerEventArgs -> unit) : Attr =
        "onpointercancel" => BindMethods.GetEventHandlerValue callback
    let pointerdown (callback: UIPointerEventArgs -> unit) : Attr =
        "onpointerdown" => BindMethods.GetEventHandlerValue callback
    let pointerenter (callback: UIPointerEventArgs -> unit) : Attr =
        "onpointerenter" => BindMethods.GetEventHandlerValue callback
    let pointerleave (callback: UIPointerEventArgs -> unit) : Attr =
        "onpointerleave" => BindMethods.GetEventHandlerValue callback
    let pointermove (callback: UIPointerEventArgs -> unit) : Attr =
        "onpointermove" => BindMethods.GetEventHandlerValue callback
    let pointerout (callback: UIPointerEventArgs -> unit) : Attr =
        "onpointerout" => BindMethods.GetEventHandlerValue callback
    let pointerover (callback: UIPointerEventArgs -> unit) : Attr =
        "onpointerover" => BindMethods.GetEventHandlerValue callback
    let pointerup (callback: UIPointerEventArgs -> unit) : Attr =
        "onpointerup" => BindMethods.GetEventHandlerValue callback
    let canplay (callback: UIEventArgs -> unit) : Attr =
        "oncanplay" => BindMethods.GetEventHandlerValue callback
    let canplaythrough (callback: UIEventArgs -> unit) : Attr =
        "oncanplaythrough" => BindMethods.GetEventHandlerValue callback
    let cuechange (callback: UIEventArgs -> unit) : Attr =
        "oncuechange" => BindMethods.GetEventHandlerValue callback
    let durationchange (callback: UIEventArgs -> unit) : Attr =
        "ondurationchange" => BindMethods.GetEventHandlerValue callback
    let emptied (callback: UIEventArgs -> unit) : Attr =
        "onemptied" => BindMethods.GetEventHandlerValue callback
    let pause (callback: UIEventArgs -> unit) : Attr =
        "onpause" => BindMethods.GetEventHandlerValue callback
    let play (callback: UIEventArgs -> unit) : Attr =
        "onplay" => BindMethods.GetEventHandlerValue callback
    let playing (callback: UIEventArgs -> unit) : Attr =
        "onplaying" => BindMethods.GetEventHandlerValue callback
    let ratechange (callback: UIEventArgs -> unit) : Attr =
        "onratechange" => BindMethods.GetEventHandlerValue callback
    let seeked (callback: UIEventArgs -> unit) : Attr =
        "onseeked" => BindMethods.GetEventHandlerValue callback
    let seeking (callback: UIEventArgs -> unit) : Attr =
        "onseeking" => BindMethods.GetEventHandlerValue callback
    let stalled (callback: UIEventArgs -> unit) : Attr =
        "onstalled" => BindMethods.GetEventHandlerValue callback
    let stop (callback: UIEventArgs -> unit) : Attr =
        "onstop" => BindMethods.GetEventHandlerValue callback
    let suspend (callback: UIEventArgs -> unit) : Attr =
        "onsuspend" => BindMethods.GetEventHandlerValue callback
    let timeupdate (callback: UIEventArgs -> unit) : Attr =
        "ontimeupdate" => BindMethods.GetEventHandlerValue callback
    let volumechange (callback: UIEventArgs -> unit) : Attr =
        "onvolumechange" => BindMethods.GetEventHandlerValue callback
    let waiting (callback: UIEventArgs -> unit) : Attr =
        "onwaiting" => BindMethods.GetEventHandlerValue callback
    let loadstart (callback: UIProgressEventArgs -> unit) : Attr =
        "onloadstart" => BindMethods.GetEventHandlerValue callback
    let timeout (callback: UIProgressEventArgs -> unit) : Attr =
        "ontimeout" => BindMethods.GetEventHandlerValue callback
    let abort (callback: UIProgressEventArgs -> unit) : Attr =
        "onabort" => BindMethods.GetEventHandlerValue callback
    let load (callback: UIProgressEventArgs -> unit) : Attr =
        "onload" => BindMethods.GetEventHandlerValue callback
    let loadend (callback: UIProgressEventArgs -> unit) : Attr =
        "onloadend" => BindMethods.GetEventHandlerValue callback
    let progress (callback: UIProgressEventArgs -> unit) : Attr =
        "onprogress" => BindMethods.GetEventHandlerValue callback
    let error (callback: UIProgressEventArgs -> unit) : Attr =
        "onerror" => BindMethods.GetEventHandlerValue callback
    let activate (callback: UIEventArgs -> unit) : Attr =
        "onactivate" => BindMethods.GetEventHandlerValue callback
    let beforeactivate (callback: UIEventArgs -> unit) : Attr =
        "onbeforeactivate" => BindMethods.GetEventHandlerValue callback
    let beforedeactivate (callback: UIEventArgs -> unit) : Attr =
        "onbeforedeactivate" => BindMethods.GetEventHandlerValue callback
    let deactivate (callback: UIEventArgs -> unit) : Attr =
        "ondeactivate" => BindMethods.GetEventHandlerValue callback
    let ended (callback: UIEventArgs -> unit) : Attr =
        "onended" => BindMethods.GetEventHandlerValue callback
    let fullscreenchange (callback: UIEventArgs -> unit) : Attr =
        "onfullscreenchange" => BindMethods.GetEventHandlerValue callback
    let fullscreenerror (callback: UIEventArgs -> unit) : Attr =
        "onfullscreenerror" => BindMethods.GetEventHandlerValue callback
    let loadeddata (callback: UIEventArgs -> unit) : Attr =
        "onloadeddata" => BindMethods.GetEventHandlerValue callback
    let loadedmetadata (callback: UIEventArgs -> unit) : Attr =
        "onloadedmetadata" => BindMethods.GetEventHandlerValue callback
    let pointerlockchange (callback: UIEventArgs -> unit) : Attr =
        "onpointerlockchange" => BindMethods.GetEventHandlerValue callback
    let pointerlockerror (callback: UIEventArgs -> unit) : Attr =
        "onpointerlockerror" => BindMethods.GetEventHandlerValue callback
    let readystatechange (callback: UIEventArgs -> unit) : Attr =
        "onreadystatechange" => BindMethods.GetEventHandlerValue callback
    let scroll (callback: UIEventArgs -> unit) : Attr =
        "onscroll" => BindMethods.GetEventHandlerValue callback
// END EVENTS