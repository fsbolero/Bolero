module rec MiniBlazor.Html

open System.Text
open System.Net
open Microsoft.JSInterop.Internal

type Attr =
    {
        Name: string
        Value: string
    }

    interface ICustomArgSerializer with
        member this.ToJsonPrimitive() =
            box (StringBuilder().Append(this).ToString())

module Attr =

    let Make name value =
        { Name = name; Value = value }

type Node =
    | Elt of name: string * attrs: list<Attr> * children: list<Node>
    | Text of text: string

    override this.ToString() = StringBuilder().Append(this).ToString()

    interface ICustomArgSerializer with
        member this.ToJsonPrimitive() =
            box (StringBuilder().Append(this).ToString())

type StringBuilder with

    member this.Append(attr: Attr) =
        this.Append(' ')
            .Append(attr.Name)
            .Append("=\"")
            .Append(WebUtility.HtmlEncode(attr.Value))
            .Append('"')

    member this.Append(attrs: list<Attr>) =
        attrs |> List.iter (this.Append >> ignore)
        this

    member this.Append(nodes: list<Node>) =
        nodes |> List.iter (this.Append >> ignore)
        this

    member this.Append(node: Node) =
        match node with
        | Elt(name, attrs, children) ->
            this.Append('<')
                .Append(name)
                .Append(attrs)
                .Append('>')
                .Append(children)
                .Append("</")
                .Append(name)
                .Append('>')
        | Text text ->
            this.Append(WebUtility.HtmlEncode(text))

let text str = Text str

let div attrs children = Elt ("div", attrs, children)
let b attrs children = Elt ("b", attrs, children)
let i attrs children = Elt ("i", attrs, children)

let style value = Attr.Make "style" value
