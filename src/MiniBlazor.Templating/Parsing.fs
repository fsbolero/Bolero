module MiniBlazor.Templating.Parsing

open System.IO
open FSharp.Quotations
open HtmlAgilityPack
type Attr = MiniBlazor.Attr
type Node = MiniBlazor.Node

let GetDoc (fileOrContent: string) =
    let doc = HtmlDocument()
    if fileOrContent.StartsWith("<") then
        doc.LoadHtml(fileOrContent)
    else
        doc.Load(fileOrContent)
    doc

let rec ParseNode (node: HtmlNode) =
    match node.NodeType with
    | HtmlNodeType.Element ->
        let name = node.Name
        let attrs =
            Expr.NewArray(typeof<Attr>,
                [ for a in node.Attributes do
                    let name = a.Name
                    let value = box a.Value
                    yield <@@ name, value @@>
                ])
        let children =
            Expr.NewArray(typeof<Node>,
                [ for c in node.ChildNodes -> ParseNode c ])
        <@@ Node.Elt(name, List.ofArray %%attrs, List.ofArray %%children) @@>
    | HtmlNodeType.Text ->
        let text = (node :?> HtmlTextNode).Text
        <@@ Node.Text text @@>
    | _ ->
        <@@ Node.Empty @@>

let ParseDoc (doc: HtmlDocument) =
    match [ for n in doc.DocumentNode.ChildNodes -> ParseNode n ] with
    | [] -> <@@ Node.Empty @@>
    | [n] -> n
    | ns ->
        let ns = Expr.NewArray(typeof<Node>, ns)
        <@@ Node.Concat(List.ofArray %%ns) @@>

let ParseFileOrContent (fileOrContent: string) =
    fileOrContent
    |> GetDoc
    |> ParseDoc
