module MiniBlazor.Render

open System.Collections.Generic
open Microsoft.JSInterop
open Microsoft.JSInterop.Internal
open MiniBlazor.Html

type RenderedNode = obj

type RNode =
    { n: string
      a: IDictionary<string, string>
      e: IDictionary<string, obj>
      c: RenderedNode[] }

let rnode name attrs events children : RenderedNode =
    box { n = name; a = attrs; e = events; c = children }

type DiffResult = obj

type InPlaceDiff =
    { a: IDictionary<string, string>
      e: IDictionary<string, obj>
      c: DiffResult[] }

type ReplaceDiff =
    { r: RenderedNode }

type InsertDiff =
    { i: RenderedNode }

type RenderedEvent =
    { mutable handler: obj -> DiffResult }

    [<JSInvokable>]
    member this.Handle(args) = this.handler(args)

let rec toRenderedNode wrapEvent = function
    | Text text -> box text
    | Elt(name, attrs, events, children) ->
        let revents = Dictionary<string, obj>()
        for KeyValue(name, handler) in events do
            let handler : RenderedEvent = { handler = wrapEvent handler }
            revents.Add(name, new DotNetObjectRef(handler))
        let rchildren = [| for c in children -> toRenderedNode wrapEvent c |]
        rnode name attrs revents rchildren

let skip = box "s"

let isSkip (x: DiffResult) =
    match x with
    | :? string as s when s = "s" -> true
    | _ -> false

let replace wrapEvent node =
    let rendered = toRenderedNode wrapEvent node
    box { r = rendered }, rendered

let insert wrapEvent node =
    let rendered = toRenderedNode wrapEvent node
    box { i = rendered }, rendered

let inPlace attrs events children =
    box { a = attrs; e = events; c = children }

let rec diff wrapEvent (before: RenderedNode) (after: Node<'Message>) =
    match before, after with
    | :? RNode as b, Elt (aname, aattrs, aevents, achildren) ->
        if aname = b.n then
            let attrDiff = Dictionary<string, string>()
            let eventDiff = Dictionary<string, obj>()
            let newEvents = Dictionary<string, obj>()
            for KeyValue(k, av) in aattrs do
                match b.a.TryGetValue(k) with
                | true, bv ->
                    if av <> bv then attrDiff.Add(k, av)
                | false, _ ->
                    attrDiff.Add(k, av)
            for KeyValue(k, av) in aevents do
                let av =
                    match b.e.TryGetValue(k) with
                    | true, bv ->
                        let bv = bv :?> DotNetObjectRef
                        (bv.Value :?> RenderedEvent).handler <- wrapEvent av
                        bv
                    | false, _ ->
                        let av = new DotNetObjectRef({ handler = wrapEvent av })
                        eventDiff.Add(k, av)
                        av
                newEvents.Add(k, av)
            for KeyValue(k, _) in b.a do
                if not (aattrs.ContainsKey k) then
                    attrDiff.Add(k, null)
            for KeyValue(k, v) in b.e do
                if not (b.e.ContainsKey k) then
                    let v = v :?> DotNetObjectRef
                    v.Dispose()
                    eventDiff.Add(k, null)
            let childDiff, newChildren =
                achildren
                |> List.mapi (fun i a ->
                    if i >= b.c.Length then
                        insert wrapEvent a
                    else
                        diff wrapEvent b.c.[i] a
                )
                |> List.unzip
            let childDiff = Array.ofList childDiff
            if  attrDiff.Count = 0 &&
                eventDiff.Count = 0 &&
                childDiff |> Array.forall (function :? string as s when s = "s" -> true | _ -> false) then
                skip, before
            else
                let diff = inPlace attrDiff eventDiff childDiff
                let newTree = rnode aname aattrs newEvents (Array.ofList newChildren)
                diff, newTree
        else
            replace wrapEvent after
    | :? string as btext, Text atext ->
        if atext = btext then
            skip, before
        else
            replace wrapEvent after
    | _ ->
        replace wrapEvent after
