module rec MiniBlazor.Html

open System
open Microsoft.AspNetCore.Blazor

type Node =
    | Empty
    | Concat of list<Node>
    | Elt of name: string * attrs: list<string * obj> * children: list<Node>
    | Text of text: string
    | Component of Type * info: ComponentInfo * attrs: list<string * obj> * children: list<Node>

    static member Collect (nodes: list<Node>) =
        nodes |> List.collect (function
            | Empty -> []
            | Concat l -> l
            | x -> [x]
        )

    static member Collect (node: Node) =
        match node with
        | Empty -> []
        | Concat l -> Node.Collect l
        | x -> [x]

and [<Struct>] ComponentInfo = { length: int }

let Element name attrs (children: list<_>) =
    Elt(name, attrs, children)

let text str = Text str
let empty = Empty
let concat nodes = Concat nodes
let comp<'T when 'T :> Components.IComponent> attrs children =
    let rec nodeLength = function
        | Empty -> 0
        | Text _ -> 1
        | Concat nodes -> List.sumBy nodeLength nodes
        | Elt (_, attrs, children) ->
            1 + Seq.length attrs + List.sumBy nodeLength children
        | Component (_, i, _, _) -> i.length
    let length = 1 + Seq.length attrs + List.sumBy nodeLength children
    Component(typeof<'T>, { length = length }, attrs, children)

// BEGIN TAGS
let a attrs children = Element "a" attrs children
let abbr attrs children = Element "abbr" attrs children
let acronym attrs children = Element "acronym" attrs children
let address attrs children = Element "address" attrs children
let applet attrs children = Element "applet" attrs children
let area attrs = Element "area" attrs []
let article attrs children = Element "article" attrs children
let aside attrs children = Element "aside" attrs children
let audio attrs children = Element "audio" attrs children
let b attrs children = Element "b" attrs children
let ``base`` attrs = Element "base" attrs []
let basefont attrs children = Element "basefont" attrs children
let bdi attrs children = Element "bdi" attrs children
let bdo attrs children = Element "bdo" attrs children
let big attrs children = Element "big" attrs children
let blockquote attrs children = Element "blockquote" attrs children
let body attrs children = Element "body" attrs children
let br attrs = Element "br" attrs []
let button attrs children = Element "button" attrs children
let canvas attrs children = Element "canvas" attrs children
let caption attrs children = Element "caption" attrs children
let center attrs children = Element "center" attrs children
let cite attrs children = Element "cite" attrs children
let code attrs children = Element "code" attrs children
let col attrs = Element "col" attrs []
let colgroup attrs children = Element "colgroup" attrs children
let content attrs children = Element "content" attrs children
let data attrs children = Element "data" attrs children
let datalist attrs children = Element "datalist" attrs children
let dd attrs children = Element "dd" attrs children
let del attrs children = Element "del" attrs children
let details attrs children = Element "details" attrs children
let dfn attrs children = Element "dfn" attrs children
let dialog attrs children = Element "dialog" attrs children
let dir attrs children = Element "dir" attrs children
let div attrs children = Element "div" attrs children
let dl attrs children = Element "dl" attrs children
let dt attrs children = Element "dt" attrs children
let element attrs children = Element "element" attrs children
let em attrs children = Element "em" attrs children
let embed attrs = Element "embed" attrs []
let fieldset attrs children = Element "fieldset" attrs children
let figcaption attrs children = Element "figcaption" attrs children
let figure attrs children = Element "figure" attrs children
let font attrs children = Element "font" attrs children
let footer attrs children = Element "footer" attrs children
let form attrs children = Element "form" attrs children
let frame attrs children = Element "frame" attrs children
let frameset attrs children = Element "frameset" attrs children
let h1 attrs children = Element "h1" attrs children
let h2 attrs children = Element "h2" attrs children
let h3 attrs children = Element "h3" attrs children
let h4 attrs children = Element "h4" attrs children
let h5 attrs children = Element "h5" attrs children
let h6 attrs children = Element "h6" attrs children
let head attrs children = Element "head" attrs children
let header attrs children = Element "header" attrs children
let hr attrs = Element "hr" attrs []
let html attrs children = Element "html" attrs children
let i attrs children = Element "i" attrs children
let iframe attrs children = Element "iframe" attrs children
let img attrs = Element "img" attrs []
let input attrs = Element "input" attrs []
let ins attrs children = Element "ins" attrs children
let kbd attrs children = Element "kbd" attrs children
let label attrs children = Element "label" attrs children
let legend attrs children = Element "legend" attrs children
let li attrs children = Element "li" attrs children
let link attrs = Element "link" attrs []
let main attrs children = Element "main" attrs children
let map attrs children = Element "map" attrs children
let mark attrs children = Element "mark" attrs children
let menu attrs children = Element "menu" attrs children
let menuitem attrs children = Element "menuitem" attrs children
let meta attrs = Element "meta" attrs []
let meter attrs children = Element "meter" attrs children
let nav attrs children = Element "nav" attrs children
let noembed attrs children = Element "noembed" attrs children
let noframes attrs children = Element "noframes" attrs children
let noscript attrs children = Element "noscript" attrs children
let object attrs children = Element "object" attrs children
let ol attrs children = Element "ol" attrs children
let optgroup attrs children = Element "optgroup" attrs children
let option attrs children = Element "option" attrs children
let output attrs children = Element "output" attrs children
let p attrs children = Element "p" attrs children
let param attrs = Element "param" attrs []
let picture attrs children = Element "picture" attrs children
let pre attrs children = Element "pre" attrs children
let progress attrs children = Element "progress" attrs children
let q attrs children = Element "q" attrs children
let rb attrs children = Element "rb" attrs children
let rp attrs children = Element "rp" attrs children
let rt attrs children = Element "rt" attrs children
let rtc attrs = Element "rtc" attrs []
let ruby attrs children = Element "ruby" attrs children
let s attrs children = Element "s" attrs children
let samp attrs children = Element "samp" attrs children
let script attrs children = Element "script" attrs children
let section attrs children = Element "section" attrs children
let select attrs children = Element "select" attrs children
let shadow attrs children = Element "shadow" attrs children
let slot attrs children = Element "slot" attrs children
let small attrs children = Element "small" attrs children
let source attrs = Element "source" attrs []
let span attrs children = Element "span" attrs children
let strike attrs children = Element "strike" attrs children
let strong attrs children = Element "strong" attrs children
let style attrs children = Element "style" attrs children
let sub attrs children = Element "sub" attrs children
let summary attrs children = Element "summary" attrs children
let sup attrs children = Element "sup" attrs children
let svg attrs children = Element "svg" attrs children
let table attrs children = Element "table" attrs children
let tbody attrs children = Element "tbody" attrs children
let td attrs children = Element "td" attrs children
let template attrs children = Element "template" attrs children
let textarea attrs children = Element "textarea" attrs children
let tfoot attrs children = Element "tfoot" attrs children
let th attrs children = Element "th" attrs children
let thead attrs children = Element "thead" attrs children
let time attrs children = Element "time" attrs children
let title attrs children = Element "title" attrs children
let tr attrs children = Element "tr" attrs children
let track attrs = Element "track" attrs []
let tt attrs children = Element "tt" attrs children
let u attrs children = Element "u" attrs children
let ul attrs children = Element "ul" attrs children
let var attrs children = Element "var" attrs children
let video attrs children = Element "video" attrs children
let wbr attrs = Element "wbr" attrs []
// END TAGS

let (=>) name value = (name, box value)

module attr =
    // BEGIN ATTRS
    let accept v = "accept" => v
    let acceptCharset v = "accept-charset" => v
    let accesskey v = "accesskey" => v
    let action v = "action" => v
    let align v = "align" => v
    let allow v = "allow" => v
    let alt v = "alt" => v
    let async v = "async" => v
    let autocapitalize v = "autocapitalize" => v
    let autocomplete v = "autocomplete" => v
    let autofocus v = "autofocus" => v
    let autoplay v = "autoplay" => v
    let bgcolor v = "bgcolor" => v
    let border v = "border" => v
    let buffered v = "buffered" => v
    let challenge v = "challenge" => v
    let charset v = "charset" => v
    let ``checked`` v = "checked" => v
    let cite v = "cite" => v
    let ``class`` v = "class" => v
    let code v = "code" => v
    let codebase v = "codebase" => v
    let color v = "color" => v
    let cols v = "cols" => v
    let colspan v = "colspan" => v
    let content v = "content" => v
    let contenteditable v = "contenteditable" => v
    let contextmenu v = "contextmenu" => v
    let controls v = "controls" => v
    let coords v = "coords" => v
    let crossorigin v = "crossorigin" => v
    let csp v = "csp" => v
    let data v = "data" => v
    let datetime v = "datetime" => v
    let decoding v = "decoding" => v
    let ``default`` v = "default" => v
    let defer v = "defer" => v
    let dir v = "dir" => v
    let dirname v = "dirname" => v
    let disabled v = "disabled" => v
    let download v = "download" => v
    let draggable v = "draggable" => v
    let dropzone v = "dropzone" => v
    let enctype v = "enctype" => v
    let ``for`` v = "for" => v
    let form v = "form" => v
    let formaction v = "formaction" => v
    let headers v = "headers" => v
    let height v = "height" => v
    let hidden v = "hidden" => v
    let high v = "high" => v
    let href v = "href" => v
    let hreflang v = "hreflang" => v
    let httpEquiv v = "http-equiv" => v
    let icon v = "icon" => v
    let id v = "id" => v
    let importance v = "importance" => v
    let integrity v = "integrity" => v
    let ismap v = "ismap" => v
    let itemprop v = "itemprop" => v
    let keytype v = "keytype" => v
    let kind v = "kind" => v
    let label v = "label" => v
    let lang v = "lang" => v
    let language v = "language" => v
    let lazyload v = "lazyload" => v
    let list v = "list" => v
    let loop v = "loop" => v
    let low v = "low" => v
    let manifest v = "manifest" => v
    let max v = "max" => v
    let maxlength v = "maxlength" => v
    let media v = "media" => v
    let method v = "method" => v
    let min v = "min" => v
    let minlength v = "minlength" => v
    let multiple v = "multiple" => v
    let muted v = "muted" => v
    let name v = "name" => v
    let novalidate v = "novalidate" => v
    let ``open`` v = "open" => v
    let optimum v = "optimum" => v
    let pattern v = "pattern" => v
    let ping v = "ping" => v
    let placeholder v = "placeholder" => v
    let poster v = "poster" => v
    let preload v = "preload" => v
    let readonly v = "readonly" => v
    let rel v = "rel" => v
    let required v = "required" => v
    let reversed v = "reversed" => v
    let rows v = "rows" => v
    let rowspan v = "rowspan" => v
    let sandbox v = "sandbox" => v
    let scope v = "scope" => v
    let selected v = "selected" => v
    let shape v = "shape" => v
    let size v = "size" => v
    let sizes v = "sizes" => v
    let slot v = "slot" => v
    let span v = "span" => v
    let spellcheck v = "spellcheck" => v
    let src v = "src" => v
    let srcdoc v = "srcdoc" => v
    let srclang v = "srclang" => v
    let srcset v = "srcset" => v
    let start v = "start" => v
    let step v = "step" => v
    let style v = "style" => v
    let summary v = "summary" => v
    let tabindex v = "tabindex" => v
    let target v = "target" => v
    let title v = "title" => v
    let translate v = "translate" => v
    let ``type`` v = "type" => v
    let usemap v = "usemap" => v
    let value v = "value" => v
    let width v = "width" => v
    let wrap v = "wrap" => v
// END ATTRS

module on =
    let event<'T when 'T :> UIEventArgs> event (callback: 'T -> unit) =
        "on" + event => Components.BindMethods.GetEventHandlerValue callback

    let change (callback: UIChangeEventArgs -> unit) =
        on.event "change" callback

    let input (message: UIChangeEventArgs -> unit) =
        on.event "input" message

    let click (message: UIMouseEventArgs -> unit) =
        on.event "click" message
