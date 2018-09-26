module MiniBlazor.Render

open System.Collections.Generic
open Microsoft.JSInterop
open Microsoft.JSInterop.Internal
open MiniBlazor.Html

type RenderedEvent =
    { mutable handler: obj -> DiffResult }

    [<JSInvokable>]
    member this.Handle(args) = this.handler(args)

and RenderedNode =
    | RText of text: string
    | RElt of name: string * attrs: IDictionary<string, string> * events: IDictionary<string, DotNetObjectRef> * children: RenderedNode[]

    interface ICustomArgSerializer with
        member this.ToJsonPrimitive() =
            match this with
            | RText t -> box t
            | RElt(name, attrs, events, children) -> box { n = name; a = attrs; e = events; c = children }

and DiffResult =
    | Skip
    | Delete
    // For `attr` and `events`, a null value means remove.
    | InPlace of attr: IDictionary<string, string> * events: IDictionary<string, DotNetObjectRef> * children: DiffResult[]
    | Replace of node: RenderedNode
    | Insert of node: RenderedNode

    interface ICustomArgSerializer with
        member this.ToJsonPrimitive() =
            match this with
            | Skip -> box "s"
            | Delete -> box "d"
            | InPlace(attr, events, children) -> box { a = attr; e = events; c = children }
            | Replace node -> box { r = node }
            | Insert node -> box { i = node }

and InPlaceResult =
    { a: IDictionary<string, string>
      e: IDictionary<string, DotNetObjectRef>
      c: DiffResult[] }

and RNode =
    { n: string
      a: IDictionary<string, string>
      e: IDictionary<string, DotNetObjectRef>
      c: RenderedNode[] }

and ReplaceResult =
    { r: RenderedNode }

and InsertResult =
    { i: RenderedNode }

let rec toRenderedNode wrapEvent = function
    | Text text -> RText text
    | Elt(name, attrs, events, children) ->
        let revents = Dictionary<string, DotNetObjectRef>()
        for KeyValue(name, handler) in events do
            let handler : RenderedEvent = { handler = wrapEvent handler }
            revents.Add(name, new DotNetObjectRef(handler))
        let rchildren = [| for c in children -> toRenderedNode wrapEvent c |]
        RElt(name, attrs, revents, rchildren)

let replace wrapEvent node =
    let rendered = toRenderedNode wrapEvent node
    Replace rendered, rendered

let insert wrapEvent node =
    let rendered = toRenderedNode wrapEvent node
    Insert rendered, rendered

let rec diff wrapEvent (before: RenderedNode) (after: Node<'Message>) =
    match before, after with
    | RElt (bname, battrs, bevents, bchildren), Elt (aname, aattrs, aevents, achildren) ->
        if aname = bname then
            let attrDiff = Dictionary<string, string>()
            let eventDiff = Dictionary<string, DotNetObjectRef>()
            let newEvents = Dictionary<string, DotNetObjectRef>()
            for KeyValue(k, av) in aattrs do
                match battrs.TryGetValue(k) with
                | true, bv ->
                    if av <> bv then attrDiff.Add(k, av)
                | false, _ ->
                    attrDiff.Add(k, av)
            for KeyValue(k, av) in aevents do
                let av =
                    match bevents.TryGetValue(k) with
                    | true, bv ->
                        (bv.Value :?> RenderedEvent).handler <- wrapEvent av
                        bv
                    | false, _ ->
                        let av = new DotNetObjectRef({ handler = wrapEvent av })
                        eventDiff.Add(k, av)
                        av
                newEvents.Add(k, av)
            for KeyValue(k, _) in battrs do
                if not (aattrs.ContainsKey k) then
                    attrDiff.Add(k, null)
            for KeyValue(k, v) in bevents do
                if not (bevents.ContainsKey k) then
                    v.Dispose()
                    eventDiff.Add(k, null)
            let childDiff, newChildren =
                achildren
                |> List.mapi (fun i a ->
                    if i >= bchildren.Length then
                        insert wrapEvent a
                    else
                        diff wrapEvent bchildren.[i] a
                )
                |> List.unzip
            let childDiff = Array.ofList childDiff
            let skip =
                attrDiff.Count = 0 &&
                eventDiff.Count = 0 &&
                childDiff |> Array.forall (function Skip -> true | _ -> false)
            if skip then
                Skip, before
            else
                let diff = InPlace(attrDiff, eventDiff, childDiff)
                let newTree = RElt(aname, aattrs, newEvents, Array.ofList newChildren)
                diff, newTree
        else
            replace wrapEvent after
    | RText btext, Text atext ->
        if atext = btext then
            Skip, before
        else
            replace wrapEvent after
    | _ ->
        replace wrapEvent after
