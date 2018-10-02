module MiniBlazor.Render

open System.Collections.Generic
open Microsoft.JSInterop
open Microsoft.JSInterop.Internal
open MiniBlazor.Html

type RenderedNode =
    | RText of text: string
    | RElt of name: string * attrs: IDictionary<string, string> * events: IDictionary<string, obj> * children: RenderedNode[]
    | RKeyedFragment of nodes: (string * RenderedNode[])[] * nodeCount: int

    static member NodeCount(node: RenderedNode) =
        match node with
        | RText _ | RElt _ -> 1
        | RKeyedFragment(_, count) -> count

    static member NodeCount(nodes: RenderedNode[]) =
        nodes |> Array.sumBy RenderedNode.NodeCount

    interface ICustomArgSerializer with
        member this.ToJsonPrimitive() =
            match this with
            | RText text -> box text
            | RElt(name, attrs, events, children) ->
                box <| dict [
                    yield "n", box name
                    if attrs.Count > 0 then yield "a", box attrs
                    if events.Count > 0 then yield "e", box events
                    if children.Length > 0 then yield "c", box children
                ]
            | RKeyedFragment(nodes, _) ->
                nodes |> Array.collect snd |> box

and DiffResult =
    | Skip of count: int
    | Delete of count: int
    | Replace of RenderedNode
    | Insert of RenderedNode
    | InPlace of attrs: IDictionary<string, string> * events: IDictionary<string, obj> * children: DiffResult[]
    | Move of from: int * count: int

    interface ICustomArgSerializer with
        member this.ToJsonPrimitive() =
            match this with
            | Skip count -> box <| dict ["s", box count]
            | Delete count -> box <| dict ["d", box count]
            | Replace node -> box <| dict ["r", box node]
            | Insert node -> box <| dict ["i", box node]
            | InPlace(attrs, events, children) ->
                box <| dict [
                    if attrs.Count > 0 then yield "a", box attrs
                    if events.Count > 0 then yield "e", box events
                    if children.Length > 0 then yield "c", box children
                ]
            | Move(from, count) ->
                box <| dict ["f", box from; "n", box count]

type RenderedEvent =
    { mutable handler: obj -> unit }

    [<JSInvokable>]
    member this.Handle(args) = this.handler(args)

let isSkip = function Skip _ -> true | _ -> false

let countActualNodesFrom i (nodes: RenderedNode[]) =
    let rec go acc i =
        if i >= nodes.Length then
            acc
        else
            go (acc + RenderedNode.NodeCount nodes.[i]) (i + 1)
    go 0 i

let rec toRenderedNode = function
    | Text text -> RText text
    | Elt(name, attrs, events, children) ->
        let revents = Dictionary<string, obj>()
        for KeyValue(name, handler) in events do
            let handler : RenderedEvent = { handler = handler }
            revents.Add(name, new DotNetObjectRef(handler))
        RElt(name, attrs, revents, toRenderedNodes children)
    | KeyedFragment nodes ->
        let mutable nodeCount = 0
        let nodes = [|
            for k, v in nodes do
                let rendered = toRenderedNodes [v]
                nodeCount <- nodeCount + RenderedNode.NodeCount rendered
                yield k, rendered
        |]
        RKeyedFragment(nodes, nodeCount)
    | Empty | Concat _ -> failwith "Should not have composite nodes at render time"

and toRenderedNodes nodes =
    let acc = ResizeArray()
    let rec go = function
        | [] -> ()
        | Empty :: rest -> go rest
        | Concat a :: rest -> go a; go rest
        | n :: rest ->
            acc.Add(toRenderedNode n)
            go rest
    go nodes
    acc.ToArray()

let replace node =
    let rendered = toRenderedNode node
    Replace rendered, rendered

let insert node =
    let rendered = toRenderedNode node
    Insert rendered, rendered

let pushDiff (diffs: ResizeArray<DiffResult>) (diff: DiffResult) =
    let lastId = diffs.Count - 1
    if lastId = -1 then diffs.Add(diff) else
    match diffs.[lastId], diff with
    | Skip a, Skip b -> diffs.[lastId] <- Skip (a + b)
    | Delete a, Delete b -> diffs.[lastId] <- Delete (a + b)
    | _ -> diffs.Add(diff)

/// Diff between the currently rendered node and the new content.
/// Assumes that `after` is a single node, ie either Text or Elt.
let rec diff (before: RenderedNode) (after: Node) : DiffResult * RenderedNode =
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
                        (bv.Value :?> RenderedEvent).handler <- av
                        bv
                    | false, _ ->
                        let av = new DotNetObjectRef({ handler = av })
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
            let childDiff, newChildren, _ = diffSiblings' 0 bchildren achildren
            if  attrDiff.Count = 0 &&
                eventDiff.Count = 0 &&
                childDiff |> Array.forall isSkip
            // If there are no changes to attributes, events, nor children, then we can skip.
            then
                Skip 1, before
            else
            // Otherwise, apply in-place changes.
                let diff = InPlace(attrDiff, eventDiff, childDiff)
                let newTree = RElt(aname, aattrs, revents, newChildren)
                diff, newTree
        else
        // Different type nodes: completely replace.
            replace after
    | RText btext, Text atext ->
        // Both text nodes: replace iff the content is different.
        if atext = btext then
            Skip 1, before
        else
            replace after
    | _ ->
        // Different types of nodes (one text and the other element): replace.
        replace after

/// Diffs between the currently rendered set of sibling nodes and the new content.
and diffSiblings' pos (before: RenderedNode[]) (after: list<Node>) : DiffResult[] * RenderedNode[] * int =
    let diffOut = ResizeArray()
    let nodeOut = ResizeArray()
    // Run in parallel through `before` (which is a plain array)
    // and `after` (which is a forest because of Concat) in prefix order.
    let rec go pos i after =
        match after with
        | [] -> pos, i
        | Empty :: arest -> go pos i arest
        | Concat a :: arest -> let pos, i = go pos i a in go pos i arest
        | KeyedFragment ak :: arest ->
            let b, j =
                if i < before.Length then
                    match before.[i] with
                    | RKeyedFragment(b, _) -> b, i + 1
                    | _ -> [||], i
                else
                    [||], i
            let diffs, res, pos = diffKeyedFragments pos b ak
            Array.iter (pushDiff diffOut) diffs
            nodeOut.Add(res)
            go pos j arest
        | (Text _ | Elt _ as a) :: arest ->
            normalGo pos i a arest
    and normalGo pos i a arest =
        let diff, node =
            if i >= before.Length then
                insert a
            else
                diff before.[i] a
        pushDiff diffOut diff
        nodeOut.Add(node)
        go (pos + 1) (i + 1) arest
    let pos, i = go pos 0 after
    let deleteCountAtEnd = countActualNodesFrom i before
    if deleteCountAtEnd > 0 then
        pushDiff diffOut (Delete deleteCountAtEnd)
    diffOut.ToArray(), nodeOut.ToArray(), pos

and diffKeyedFragments pos (before: (string * RenderedNode[])[]) (after: list<string * Node>) =
    let diffOut = ResizeArray()
    let nodeOut = ResizeArray()
    let handledBefore = HashSet()
    let afterKeys = HashSet()
    for k, _ in after do afterKeys.Add(k) |> ignore
    let addHandled k =
        let res = handledBefore.Add(k)
        if not res then printfn "Warning: duplicate key: %s" k
        res
    let mutable nodeCount = 0
    let pushNode (node: string * RenderedNode[]) =
        nodeCount <- nodeCount + RenderedNode.NodeCount (snd node)
        nodeOut.Add(node)
    let rec tryFindBefore pos i k =
        if i >= before.Length then None else
        let bk, b = before.[i]
        if handledBefore.Contains bk then
            tryFindBefore pos (i + 1) k
        elif bk = k then
            Some (pos, b)
        else
            let count = RenderedNode.NodeCount b
            tryFindBefore (pos + count) (i + 1) k
    // Run in parallel through `before` (which is a plain array)
    // and `after` (which is a forest because of Concat) in prefix order.
    let rec go pos i after =
        if i < before.Length && handledBefore.Contains(fst before.[i]) then
            go pos (i + 1) after
        else
        match after with
        | [] -> pos, i
        | (ak, a) :: arest ->
            if i < before.Length then
                let bk, b = before.[i]
                if bk = ak then
                    if addHandled ak then
                        let diffs, nodes, pos = diffSiblings' pos b [a]
                        Array.iter (pushDiff diffOut) diffs
                        pushNode (ak, nodes)
                        go pos (i + 1) arest
                    else
                        go pos (i + 1) arest
                elif afterKeys.Contains(bk) then
                    nonImmediateGo pos i ak a arest
                else
                    addHandled bk |> ignore
                    pushDiff diffOut (Delete (RenderedNode.NodeCount b))
                    go pos (i + 1) after
            else
                nonImmediateGo pos i ak a arest
    and nonImmediateGo pos i ak a arest =
        match tryFindBefore pos i ak with
        | Some (mpos, mb) ->
            if addHandled ak then
                pushDiff diffOut (Move(mpos, RenderedNode.NodeCount mb))
                let diffs, nodes, pos = diffSiblings' pos mb [a]
                Array.iter (pushDiff diffOut) diffs
                nodeOut.Add((ak, nodes))
                go pos i arest
            else
                go pos i arest
        | None ->
            let nodes = toRenderedNodes [a]
            for node in nodes do pushDiff diffOut (Insert node)
            nodeOut.Add((ak, nodes))
            go (pos + RenderedNode.NodeCount nodes) i arest
    let pos, i = go pos 0 after
    let deleteCountAtEnd =
        before |> Array.sumBy (fun (k, nodes) ->
            if handledBefore.Contains(k) then
                0
            else
                RenderedNode.NodeCount nodes)
    if deleteCountAtEnd > 0 then
        pushDiff diffOut (Delete deleteCountAtEnd)
    diffOut.ToArray(), RKeyedFragment (nodeOut.ToArray(), nodeCount), pos

let diffSiblings before after =
    let diff, nodes, _ = diffSiblings' 0 before after
    diff, nodes
