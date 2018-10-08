module rec MiniBlazor.Html

open Microsoft.AspNetCore.Blazor

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
    Node.Component(typeof<'T>, { length = length }, attrs, children)

// BEGIN TAGS
let a attrs children = Node.Elt("a", attrs, children)
let abbr attrs children = Node.Elt("abbr", attrs, children)
let acronym attrs children = Node.Elt("acronym", attrs, children)
let address attrs children = Node.Elt("address", attrs, children)
let applet attrs children = Node.Elt("applet", attrs, children)
let area attrs = Node.Elt("area", attrs, [])
let article attrs children = Node.Elt("article", attrs, children)
let aside attrs children = Node.Elt("aside", attrs, children)
let audio attrs children = Node.Elt("audio", attrs, children)
let b attrs children = Node.Elt("b", attrs, children)
let ``base`` attrs = Node.Elt("base", attrs, [])
let basefont attrs children = Node.Elt("basefont", attrs, children)
let bdi attrs children = Node.Elt("bdi", attrs, children)
let bdo attrs children = Node.Elt("bdo", attrs, children)
let big attrs children = Node.Elt("big", attrs, children)
let blockquote attrs children = Node.Elt("blockquote", attrs, children)
let body attrs children = Node.Elt("body", attrs, children)
let br attrs = Node.Elt("br", attrs, [])
let button attrs children = Node.Elt("button", attrs, children)
let canvas attrs children = Node.Elt("canvas", attrs, children)
let caption attrs children = Node.Elt("caption", attrs, children)
let center attrs children = Node.Elt("center", attrs, children)
let cite attrs children = Node.Elt("cite", attrs, children)
let code attrs children = Node.Elt("code", attrs, children)
let col attrs = Node.Elt("col", attrs, [])
let colgroup attrs children = Node.Elt("colgroup", attrs, children)
let content attrs children = Node.Elt("content", attrs, children)
let data attrs children = Node.Elt("data", attrs, children)
let datalist attrs children = Node.Elt("datalist", attrs, children)
let dd attrs children = Node.Elt("dd", attrs, children)
let del attrs children = Node.Elt("del", attrs, children)
let details attrs children = Node.Elt("details", attrs, children)
let dfn attrs children = Node.Elt("dfn", attrs, children)
let dialog attrs children = Node.Elt("dialog", attrs, children)
let dir attrs children = Node.Elt("dir", attrs, children)
let div attrs children = Node.Elt("div", attrs, children)
let dl attrs children = Node.Elt("dl", attrs, children)
let dt attrs children = Node.Elt("dt", attrs, children)
let element attrs children = Node.Elt("element", attrs, children)
let em attrs children = Node.Elt("em", attrs, children)
let embed attrs = Node.Elt("embed", attrs, [])
let fieldset attrs children = Node.Elt("fieldset", attrs, children)
let figcaption attrs children = Node.Elt("figcaption", attrs, children)
let figure attrs children = Node.Elt("figure", attrs, children)
let font attrs children = Node.Elt("font", attrs, children)
let footer attrs children = Node.Elt("footer", attrs, children)
let form attrs children = Node.Elt("form", attrs, children)
let frame attrs children = Node.Elt("frame", attrs, children)
let frameset attrs children = Node.Elt("frameset", attrs, children)
let h1 attrs children = Node.Elt("h1", attrs, children)
let h2 attrs children = Node.Elt("h2", attrs, children)
let h3 attrs children = Node.Elt("h3", attrs, children)
let h4 attrs children = Node.Elt("h4", attrs, children)
let h5 attrs children = Node.Elt("h5", attrs, children)
let h6 attrs children = Node.Elt("h6", attrs, children)
let head attrs children = Node.Elt("head", attrs, children)
let header attrs children = Node.Elt("header", attrs, children)
let hr attrs = Node.Elt("hr", attrs, [])
let html attrs children = Node.Elt("html", attrs, children)
let i attrs children = Node.Elt("i", attrs, children)
let iframe attrs children = Node.Elt("iframe", attrs, children)
let img attrs = Node.Elt("img", attrs, [])
let input attrs = Node.Elt("input", attrs, [])
let ins attrs children = Node.Elt("ins", attrs, children)
let kbd attrs children = Node.Elt("kbd", attrs, children)
let label attrs children = Node.Elt("label", attrs, children)
let legend attrs children = Node.Elt("legend", attrs, children)
let li attrs children = Node.Elt("li", attrs, children)
let link attrs = Node.Elt("link", attrs, [])
let main attrs children = Node.Elt("main", attrs, children)
let map attrs children = Node.Elt("map", attrs, children)
let mark attrs children = Node.Elt("mark", attrs, children)
let menu attrs children = Node.Elt("menu", attrs, children)
let menuitem attrs children = Node.Elt("menuitem", attrs, children)
let meta attrs = Node.Elt("meta", attrs, [])
let meter attrs children = Node.Elt("meter", attrs, children)
let nav attrs children = Node.Elt("nav", attrs, children)
let noembed attrs children = Node.Elt("noembed", attrs, children)
let noframes attrs children = Node.Elt("noframes", attrs, children)
let noscript attrs children = Node.Elt("noscript", attrs, children)
let object attrs children = Node.Elt("object", attrs, children)
let ol attrs children = Node.Elt("ol", attrs, children)
let optgroup attrs children = Node.Elt("optgroup", attrs, children)
let option attrs children = Node.Elt("option", attrs, children)
let output attrs children = Node.Elt("output", attrs, children)
let p attrs children = Node.Elt("p", attrs, children)
let param attrs = Node.Elt("param", attrs, [])
let picture attrs children = Node.Elt("picture", attrs, children)
let pre attrs children = Node.Elt("pre", attrs, children)
let progress attrs children = Node.Elt("progress", attrs, children)
let q attrs children = Node.Elt("q", attrs, children)
let rb attrs children = Node.Elt("rb", attrs, children)
let rp attrs children = Node.Elt("rp", attrs, children)
let rt attrs children = Node.Elt("rt", attrs, children)
let rtc attrs = Node.Elt("rtc", attrs, [])
let ruby attrs children = Node.Elt("ruby", attrs, children)
let s attrs children = Node.Elt("s", attrs, children)
let samp attrs children = Node.Elt("samp", attrs, children)
let script attrs children = Node.Elt("script", attrs, children)
let section attrs children = Node.Elt("section", attrs, children)
let select attrs children = Node.Elt("select", attrs, children)
let shadow attrs children = Node.Elt("shadow", attrs, children)
let slot attrs children = Node.Elt("slot", attrs, children)
let small attrs children = Node.Elt("small", attrs, children)
let source attrs = Node.Elt("source", attrs, [])
let span attrs children = Node.Elt("span", attrs, children)
let strike attrs children = Node.Elt("strike", attrs, children)
let strong attrs children = Node.Elt("strong", attrs, children)
let style attrs children = Node.Elt("style", attrs, children)
let sub attrs children = Node.Elt("sub", attrs, children)
let summary attrs children = Node.Elt("summary", attrs, children)
let sup attrs children = Node.Elt("sup", attrs, children)
let svg attrs children = Node.Elt("svg", attrs, children)
let table attrs children = Node.Elt("table", attrs, children)
let tbody attrs children = Node.Elt("tbody", attrs, children)
let td attrs children = Node.Elt("td", attrs, children)
let template attrs children = Node.Elt("template", attrs, children)
let textarea attrs children = Node.Elt("textarea", attrs, children)
let tfoot attrs children = Node.Elt("tfoot", attrs, children)
let th attrs children = Node.Elt("th", attrs, children)
let thead attrs children = Node.Elt("thead", attrs, children)
let time attrs children = Node.Elt("time", attrs, children)
let title attrs children = Node.Elt("title", attrs, children)
let tr attrs children = Node.Elt("tr", attrs, children)
let track attrs = Node.Elt("track", attrs, [])
let tt attrs children = Node.Elt("tt", attrs, children)
let u attrs children = Node.Elt("u", attrs, children)
let ul attrs children = Node.Elt("ul", attrs, children)
let var attrs children = Node.Elt("var", attrs, children)
let video attrs children = Node.Elt("video", attrs, children)
let wbr attrs = Node.Elt("wbr", attrs, [])
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
