module Bolero.Templating.ConvertExprSequential

open FSharp.Quotations
open Bolero
open Bolero.TemplatingInternals
open Microsoft.AspNetCore.Components.Rendering

#nowarn "3220" // Using .Item1 instead of fst inside quotations for nicer output IL

/// Map an expression's vars from its parent, wrapping the expression in let declarations.
let wrapAndConvert (vars: Map<string, Expr>) (subst: list<Parsing.VarSubstitution>) convert expr i =
    let vars, addLets = ((vars, id), subst) ||> List.fold (fun (vars, addLets) wrap ->
        let unwrapped = vars[wrap.name]
        let wrapped = ConvertExpr.WrapExpr wrap.innerType wrap.outerType unwrapped
        let var = Var(wrap.name, ConvertExpr.TypeOf wrap.innerType)
        let addLets e = Expr.Let(var, defaultArg wrapped unwrapped, addLets e) |> Expr.Cast
        let vars = Map.add wrap.name (Expr.Var var) vars
        (vars, addLets)
    )
    let expr, i = convert vars expr i
    addLets expr, i

let convertSequence f l i =
    ((<@ () @>, i), l)
    ||> Seq.fold (fun (acc, i) e ->
        let e, i = f e i
        <@ %acc; %e @>, i)

let rec convertAttr (r: Expr<obj>) (b: Expr<RenderTreeBuilder>) (vars: Map<string, Expr>) (attr: Parsing.Expr) (i: int) : Expr<unit> * int =
    match attr with
    | Parsing.Concat attrs ->
        (attrs, i) ||> convertSequence (convertAttr r b vars)
    | Parsing.Attr (name, value) ->
        let value = ConvertExpr.ConvertAttrValue vars value
        <@ (%b).AddAttribute(i, name, %value) @>, i + 1
    | Parsing.EventHandler (name, value, argTy) ->
        let value = ConvertExpr.ConvertAttrValue vars value
        let cb = TExpr.Coerce<obj>(Expr.Application(Expr.Coerce(value, ConvertExpr.EventCallbackOf argTy), r))
        <@ (%b).AddAttribute(i, name, %cb) @>, i + 1
    | Parsing.WrapVars (subst, attr) ->
        wrapAndConvert vars subst (convertAttr r b) attr i
    | Parsing.HtmlRef varName ->
        let ref = vars[varName] |> Expr.Cast<HtmlRef>
        <@ Ref.MakeAttr(%ref).Invoke(%r, %b, i) |> ignore @>, i + 1
    | Parsing.VarContent _ ->
        failwith "Impossible: should be going through ConvertExpr"
    | Parsing.Fst _ | Parsing.Snd _ | Parsing.PlainHtml _ | Parsing.Elt _ ->
        failwith $"Invalid attribute: {attr}"

let rec convertNode (r: Expr<obj>) (b: Expr<RenderTreeBuilder>) (vars: Map<string, Expr>) (node: Parsing.Expr) (i: int) : Expr<unit> * int =
    match node with
    | Parsing.Concat exprs ->
        (exprs, i) ||> convertSequence (convertNode r b vars)
    | Parsing.PlainHtml string ->
        <@ (%b).AddMarkupContent(i, string) @>, i + 1
    | Parsing.Elt (name, attrs, children) ->
        let openElt, i = <@ (%b).OpenElement(i, name) @>, i + 1
        let cssScope, i = <@ Nodes.AddCssScope(%r, %b, i) @>, i + 1
        let attrs = attrs |> Seq.sortBy (function Parsing.HtmlRef _ -> 1 | _ -> 0)
        let attrs, i = (attrs, i) ||> convertSequence (convertAttr r b vars)
        let children, i = (children, i) ||> convertSequence (convertNode r b vars)
        <@
            %openElt
            %cssScope
            %attrs
            %children
            (%b).CloseElement()
        @>, i
    | Parsing.VarContent varName ->
        let node = vars[varName] |> Expr.Cast<Node>
        <@
            (%b).OpenRegion(i)
            (%node).Invoke(%r, %b, 0) |> ignore
            (%b).CloseRegion()
        @>, i + 1
    | Parsing.WrapVars (subst, node) ->
        wrapAndConvert vars subst (convertNode r b) node i
    | Parsing.Fst _ | Parsing.Snd _ | Parsing.Attr _ | Parsing.EventHandler _ | Parsing.HtmlRef _ ->
        failwith $"Invalid node: {node}"

let ConvertNode (vars: Map<string, Expr>) (node: Parsing.Expr) : Expr<Node> =
    let r = Var("r", typeof<obj>, false)
    let b = Var("b", typeof<RenderTreeBuilder>, false)
    Expr.Call(
        typeof<Nodes>.GetMethod("Region"),
        [Expr.Lambda(r, Expr.Lambda(b,
            let r = Expr.Var r |> Expr.Cast
            let b = Expr.Var b |> Expr.Cast
            convertNode r b vars node 0 |> fst))])
    |> Expr.Cast
