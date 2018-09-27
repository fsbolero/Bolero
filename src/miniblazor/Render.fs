module MiniBlazor.Render

open System.Collections.Generic
open Microsoft.JSInterop
open Microsoft.JSInterop.Internal
open MiniBlazor.Html

type RenderedNode =
    | RText of text: string
    | RElt of name: string * attrs: IDictionary<string, string> * events: IDictionary<string, obj> * children: RenderedNode[]
    | RKeyedFragment of nodes: (string * RenderedNode[])[]

    interface ICustomArgSerializer with
        member this.ToJsonPrimitive() =
            match this with
            | RText text -> box text
            | RElt(name, attrs, events, children) ->
                box { n = name; a = attrs; e = events; c = children }
            | RKeyedFragment nodes ->
                nodes |> Array.collect snd |> box

and DiffResult =
    | Skip
    | Delete of count: int
    | Replace of RenderedNode
    | Insert of RenderedNode
    | InPlace of attrs: IDictionary<string, string> * events: IDictionary<string, obj> * children: DiffResult[]
    | Move of from: int * count: int

    interface ICustomArgSerializer with
        member this.ToJsonPrimitive() =
            match this with
            | Skip -> box "s"
            | Delete count -> box { d = count }
            | Replace node -> box { r = node }
            | Insert node -> box { i = node }
            | InPlace(attrs, events, children) ->
                box { a = attrs; e = events; c = children }
            | Move(from, count) ->
                box { f = from; n = count }

and RNode =
    { n: string
      a: IDictionary<string, string>
      e: IDictionary<string, obj>
      c: RenderedNode[] }

and InPlaceDiff =
    { a: IDictionary<string, string>
      e: IDictionary<string, obj>
      c: DiffResult[] }

and DeleteDiff =
    { d: int }

and MoveDiff =
    { f: int
      n: int }

and ReplaceDiff =
    { r: RenderedNode }

and InsertDiff =
    { i: RenderedNode }

type RenderedEvent =
    { mutable handler: obj -> DiffResult[] }

    [<JSInvokable>]
    member this.Handle(args) = this.handler(args)

let isSkip = function Skip -> true | _ -> false

let rec countActualNodes = function
    | RText _ | RElt _ -> 1
    | RKeyedFragment es ->
        es |> Array.sumBy (fun (_, n) ->
            Array.sumBy countActualNodes n)

let countActualNodesFrom i (nodes: RenderedNode[]) =
    let rec go acc i =
        if i >= nodes.Length then
            acc
        else
            go (acc + countActualNodes nodes.[i]) (i + 1)
    go 0 i

type Renderer<'Message>(wrapEvent) =

    let rec toRenderedNode = function
        | Text text -> RText text
        | Elt(name, attrs, events, children) ->
            let revents = Dictionary<string, obj>()
            for KeyValue(name, handler) in events do
                let handler : RenderedEvent = { handler = wrapEvent handler }
                revents.Add(name, new DotNetObjectRef(handler))
            RElt(name, attrs, revents, toRenderedNodes children)
        | KeyedFragment nodes ->
            RKeyedFragment [| for k, v in nodes -> k, toRenderedNodes [v] |]
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

    /// Diff between the currently rendered node and the new content.
    /// Assumes that `after` is a single node, ie either Text or Elt.
    let rec diff (before: RenderedNode) (after: Node<'Message>) : DiffResult * RenderedNode =
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
                let childDiff, newChildren, _, _ = diffSiblings 0 bchildren achildren
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
                replace after
        | RText btext, Text atext ->
            // Both text nodes: replace iff the content is different.
            if atext = btext then
                Skip, before
            else
                replace after
        | _ ->
            // Different types of nodes (one text and the other element): replace.
            replace after

    /// Diffs between the currently rendered set of sibling nodes and the new content.
    and diffSiblings pos (before: RenderedNode[]) (after: list<Node<'Message>>) : DiffResult[] * RenderedNode[] * int * int =
        let diffOut = ResizeArray()
        let nodeOut = ResizeArray()
        // Run in parallel through `before` (which is a plain array)
        // and `after` (which is a forest because of Concat) in prefix order.
        let rec go pos i after =
            match after with
            | [] -> pos, i
            | Empty :: arest -> go pos i arest
            | Concat a :: arest -> let pos, i = go pos i a in go pos i arest
            | KeyedFragment ak as a :: arest ->
                if i < before.Length then
                    match before.[i] with
                    | RKeyedFragment b ->
                        let diffs, res, deleteCount, pos = diffKeyedFragments pos b ak
                        diffOut.AddRange(diffs)
                        if deleteCount > 0 then diffOut.Add(Delete deleteCount)
                        nodeOut.Add(RKeyedFragment res)
                        go pos (i + 1) arest
                    | _ -> normalGo pos i a arest
                else
                    normalGo pos i a arest
            | (Text _ | Elt _ as a) :: arest ->
                normalGo pos i a arest
        and normalGo pos i a arest =
            let diff, node =
                if i >= before.Length then
                    insert a
                else
                    diff before.[i] a
            diffOut.Add(diff)
            nodeOut.Add(node)
            go (pos + 1) (i + 1) arest
        let pos, i = go pos 0 after
        diffOut.ToArray(), nodeOut.ToArray(), countActualNodesFrom i before, pos

    and diffKeyedFragments pos (before: (string * RenderedNode[])[]) (after: list<string * Node<'Message>>) =
        let diffOut = ResizeArray()
        let nodeOut = ResizeArray()
        let handledBefore = HashSet()
        let afterKeys = HashSet()
        for k, _ in after do afterKeys.Add(k) |> ignore
        let addHandled k =
            let res = handledBefore.Add(k)
            if not res then printfn "Warning: duplicate key: %s" k
            res
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
                            let diffs, nodes, deleteCount, pos = diffSiblings pos b [a]
                            diffOut.AddRange(diffs)
                            if deleteCount > 0 then diffOut.Add(Delete deleteCount)
                            nodeOut.Add((ak, nodes))
                            go pos (i + 1) arest
                        else
                            go pos (i + 1) arest
                    elif afterKeys.Contains(bk) then
                        nonImmediateGo pos i ak a arest
                    else
                        addHandled bk |> ignore
                        diffOut.Add(Delete (Array.sumBy countActualNodes b))
                        go pos (i + 1) after
                else
                    nonImmediateGo pos i ak a arest
        and nonImmediateGo pos i ak a arest =
            match tryFindBefore pos i ak with
            | Some (mpos, mi) ->
                if addHandled ak then
                    let _, b = before.[mi]
                    diffOut.Add(Move(mpos, Array.sumBy countActualNodes b))
                    let diffs, nodes, deleteCount, pos = diffSiblings pos b [a]
                    diffOut.AddRange(diffs)
                    if deleteCount > 0 then diffOut.Add(Delete deleteCount)
                    nodeOut.Add((ak, nodes))
                    go pos (i + 1) arest
                else
                    go pos (i + 1) arest
            | None ->
                let nodes = toRenderedNodes [a]
                for node in nodes do diffOut.Add(Insert node)
                nodeOut.Add((ak, nodes))
                go (pos + Array.sumBy countActualNodes nodes) i arest
        and tryFindBefore pos i k =
            if i >= before.Length then None else
            let bk, b = before.[i]
            if bk = k then
                Some (pos, i)
            else
                tryFindBefore (pos + Array.sumBy countActualNodes b) (i + 1) k
        let pos, i = go pos 0 after
        let deleteCount =
            before |> Array.sumBy (fun (k, nodes) ->
                if handledBefore.Contains(k) then
                    0
                else
                    Array.sumBy countActualNodes nodes)
        diffOut.ToArray(), nodeOut.ToArray(), deleteCount, pos

    member this.ToRenderedNodes(nodes) = toRenderedNodes nodes

    member this.DiffSiblings(before, after) =
        let diff, nodes, _, _ = diffSiblings 0 before after
        diff, nodes
