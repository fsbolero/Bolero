module MiniBlazor.Render

open System.Collections.Generic
open Microsoft.JSInterop
open Microsoft.JSInterop.Internal
open MiniBlazor.Html

type RenderedNode =
    | RText of text: string
    | RElt of name: string * attrs: IDictionary<string, string> * events: IDictionary<string, obj> * children: RenderedNode[]

    interface ICustomArgSerializer with
        member this.ToJsonPrimitive() =
            match this with
            | RText text -> box text
            | RElt(name, attrs, events, children) ->
                box { n = name; a = attrs; e = events; c = children }

and DiffResult =
    | Skip
    | Replace of RenderedNode
    | Insert of RenderedNode
    | InPlace of attrs: IDictionary<string, string> * events: IDictionary<string, obj> * children: DiffResult[]

    interface ICustomArgSerializer with
        member this.ToJsonPrimitive() =
            match this with
            | Skip -> box "s"
            | Replace node -> box { r = node }
            | Insert node -> box { i = node }
            | InPlace(attrs, events, children) ->
                box { a = attrs; e = events; c = children }

and RNode =
    { n: string
      a: IDictionary<string, string>
      e: IDictionary<string, obj>
      c: RenderedNode[] }

and InPlaceDiff =
    { a: IDictionary<string, string>
      e: IDictionary<string, obj>
      c: DiffResult[] }

and ReplaceDiff =
    { r: RenderedNode }

and InsertDiff =
    { i: RenderedNode }

type RenderedEvent =
    { mutable handler: obj -> DiffResult[] }

    [<JSInvokable>]
    member this.Handle(args) = this.handler(args)

let rec toRenderedNode wrapEvent = function
    | Text text -> RText text
    | Elt(name, attrs, events, children) ->
        let revents = Dictionary<string, obj>()
        for KeyValue(name, handler) in events do
            let handler : RenderedEvent = { handler = wrapEvent handler }
            revents.Add(name, new DotNetObjectRef(handler))
        RElt(name, attrs, revents, toRenderedNodes wrapEvent children)
    | Empty | Concat _ -> failwith "Should not have composite nodes at render time"

and toRenderedNodes wrapEvent nodes =
    let acc = ResizeArray()
    let rec go = function
        | [] -> ()
        | Empty :: rest -> go rest
        | Concat a :: rest -> go a; go rest
        | n :: rest ->
            acc.Add(toRenderedNode wrapEvent n)
            go rest
    go nodes
    acc.ToArray()

let isSkip = function Skip -> true | _ -> false

let replace wrapEvent node =
    let rendered = toRenderedNode wrapEvent node
    Replace rendered, rendered

let insert wrapEvent node =
    let rendered = toRenderedNode wrapEvent node
    Insert rendered, rendered

/// Diff between the currently rendered node and the new content.
/// Assumes that `after` is a single node, ie either Text or Elt.
let rec private diff wrapEvent (before: RenderedNode) (after: Node<'Message>) : DiffResult * RenderedNode =
    match before, after with
    | RElt(bname, battrs, bevents, bchildren), Elt (aname, aattrs, aevents, achildren) ->
        if aname = bname then
        // Same node type: apply in-place changes.
            let attrDiff = Dictionary<string, string>() // Attribute diff: null to remove, value to set.
            let eventDiff = Dictionary<string, obj>() // Event diff: null to remove, value to set.
            let revents = Dictionary<string, obj>() // Wrapped events for the final RenderedNode.
            // 1. Diff attributes
            //   1.1. Add attributes that weren't there yet or have changed.
            for KeyValue(k, av) in aattrs do
                match battrs.TryGetValue(k) with
                | true, bv ->
                    if av <> bv then attrDiff.Add(k, av)
                | false, _ ->
                    attrDiff.Add(k, av)
            //   1.2. Remove attributes that won't be there anymore.
            for KeyValue(k, _) in battrs do
                if not (aattrs.ContainsKey k) then
                    attrDiff.Add(k, null)
            // 2. Diff events
            //   2.1. Add events that weren't there yet.
            for KeyValue(k, av) in aevents do
                let av =
                    match bevents.TryGetValue(k) with
                    | true, bv ->
                        // If the event already existed, we can just set the callback locally.
                        let bv = bv :?> DotNetObjectRef
                        (bv.Value :?> RenderedEvent).handler <- wrapEvent av
                        bv
                    | false, _ ->
                        let av = new DotNetObjectRef({ handler = wrapEvent av })
                        eventDiff.Add(k, av)
                        av
                revents.Add(k, av)
            //   2.2. Remove events that aren't there anymore.
            for KeyValue(k, v) in bevents do
                if not (bevents.ContainsKey k) then
                    let v = v :?> DotNetObjectRef
                    v.Dispose()
                    eventDiff.Add(k, null)
            // 3. Diff children.
            let childDiff, newChildren = diffSiblings wrapEvent bchildren achildren
            if  attrDiff.Count = 0 &&
                eventDiff.Count = 0 &&
                childDiff |> Array.forall isSkip
            // If there are no changes to attributes, events, nor children, then we can skip.
            then
                Skip, before
            else
            // Otherwise, apply in-place changes.
                let diff = InPlace(attrDiff, eventDiff, childDiff)
                let newTree = RElt(aname, aattrs, revents, newChildren)
                diff, newTree
        else
        // Different type nodes: completely replace.
            replace wrapEvent after
    | RText btext, Text atext ->
        // Both text nodes: replace iff the content is different.
        if atext = btext then
            Skip, before
        else
            replace wrapEvent after
    | _ ->
        // Different types of nodes (one text and the other element): replace.
        replace wrapEvent after

/// Diffs between the currently rendered set of sibling nodes and the new content.
and diffSiblings wrapEvent (before: RenderedNode[]) (after: list<Node<'Message>>) : DiffResult[] * RenderedNode[] =
    let acc = ResizeArray()
    // Run in parallel through `before` (which is a plain array)
    // and `after` (which is a forest because of Concat) in prefix order.
    let rec go i = function
        | [] -> i
        | Empty :: arest -> go i arest
        | Concat a :: arest -> go (go i a) arest
        | a :: arest ->
            if i >= before.Length then
                insert wrapEvent a
            else
                diff wrapEvent before.[i] a
            |> acc.Add
            go (i + 1) arest
    go 0 after |> ignore
    acc.ToArray() |> Array.unzip
