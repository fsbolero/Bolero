// Due to this issue, we need to reimplement a variant of Program.runWith:
// https://github.com/elmish/elmish/issues/210
// As soon as the above issue is solved, this file can go.
namespace Elmish

[<Struct>]
type internal RingState<'item> =
    | Writable of wx:'item array * ix:int
    | ReadWritable of rw:'item array * wix:int * rix:int

type internal RingBuffer<'item>(size) =
    let doubleSize ix (items: 'item array) =
        seq { yield! items |> Seq.skip ix
              yield! items |> Seq.take ix
              for _ in 0..items.Length do
                yield Unchecked.defaultof<'item> }
        |> Array.ofSeq

    let mutable state : 'item RingState =
        Writable (Array.zeroCreate (max size 10), 0)

    member _.Pop() =
        match state with
        | ReadWritable (items, wix, rix) ->
            let rix' = (rix + 1) % items.Length
            match rix' = wix with
            | true ->
                state <- Writable(items, wix)
            | _ ->
                state <- ReadWritable(items, wix, rix')
            Some items[rix]
        | _ ->
            None

    member _.Push (item:'item) =
        match state with
        | Writable (items, ix) ->
            items[ix] <- item
            let wix = (ix + 1) % items.Length
            state <- ReadWritable(items, wix, ix)
        | ReadWritable (items, wix, rix) ->
            items[wix] <- item
            let wix' = (wix + 1) % items.Length
            match wix' = rix with
            | true ->
                state <- ReadWritable(items |> doubleSize rix, items.Length, 0)
            | _ ->
                state <- ReadWritable(items, wix', rix)

// In this module we split Elmish's Program.runWith into two parts:
// 1. The first rendering, which must be done in OnInitialized()
// 2. The execution of init cmd and subscriptions, which must be done in OnAfterRenderAsync(true)
module internal Program' =

    module Cmd =
        let internal exec onError (dispatch: Dispatch<'msg>) (cmd: Cmd<'msg>) =
            cmd |> List.iter (fun call -> try call dispatch with ex -> onError ex)

    module Subs = Sub.Internal

    let runFirstRender (arg: 'arg) (program: Program<'arg, 'model, 'msg, 'view>) =
        // An ugly way to extract properties from the program because they're private
        let init = Program.init program
        let update = Program.update program
        let setState = Program.setState program
        let onError = Program.onError program
        let mutable subscribe = fun _ -> []
        let mutable termination = (fun _ -> false), ignore
        program
        |> Program.mapSubscription (fun f -> subscribe <- f; f)
        |> Program.mapTermination (fun f -> termination <- f; f)
        |> ignore

        let model,cmd = init arg
        let sub = subscribe model
        let toTerminate, terminate = termination
        let rb = RingBuffer 10
        let mutable reentered = false
        let mutable state = model
        let mutable activeSubs = Subs.empty
        let mutable terminated = false
        let rec dispatch msg =
            if not terminated then
                rb.Push msg
                if not reentered then
                    reentered <- true
                    processMsgs ()
                    reentered <- false
        and processMsgs () =
            let mutable nextMsg = rb.Pop()
            while not terminated && Option.isSome nextMsg do
                let msg = nextMsg.Value
                if toTerminate msg then
                    Subs.Fx.stop onError activeSubs
                    terminate state
                    terminated <- true
                else
                    let model',cmd' = update msg state
                    let sub' = subscribe model'
                    setState model' dispatch
                    cmd' |> Cmd.exec (fun ex -> onError ( $"Error handling the message: %A{msg}", ex)) dispatch
                    state <- model'
                    activeSubs <- Subs.diff activeSubs sub' |> Subs.Fx.change onError dispatch
                    nextMsg <- rb.Pop()

        reentered <- true
        setState model dispatch
        let mutable cmd = cmd

        let updateInitState m cmd' =
            setState m dispatch
            state <- m
            cmd <- cmd @ cmd'

        let run () =
            cmd |> Cmd.exec (fun ex -> onError ("Error intitializing:", ex)) dispatch
            activeSubs <- Subs.diff activeSubs sub |> Subs.Fx.change onError dispatch
            processMsgs ()
            reentered <- false

        updateInitState, model, run
