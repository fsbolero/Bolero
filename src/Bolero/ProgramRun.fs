// Due to this issue, we need to reimplement a variant of Program.runWith:
// https://github.com/elmish/elmish/issues/210
// As soon as the above issue is solved, this file can go.
namespace Elmish
open System

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

    member __.Pop() =
        match state with
        | ReadWritable (items, wix, rix) ->
            let rix' = (rix + 1) % items.Length
            match rix' = wix with
            | true ->
                state <- Writable(items, wix)
            | _ ->
                state <- ReadWritable(items, wix, rix')
            Some items.[rix]
        | _ ->
            None

    member __.Push (item:'item) =
        match state with
        | Writable (items, ix) ->
            items.[ix] <- item
            let wix = (ix + 1) % items.Length
            state <- ReadWritable(items, wix, ix)
        | ReadWritable (items, wix, rix) ->
            items.[wix] <- item
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

    let runFirstRender (arg: 'arg) (program: Program<'arg, 'model, 'msg, 'view>) =
        // An ugly way to extract init and update because they're private
        let mutable init = Unchecked.defaultof<_>
        let mutable update = Unchecked.defaultof<_>
        let mutable subscribe = Unchecked.defaultof<_>
        let program =
            program
            |> Program.map
                (fun i -> init <- i; i)
                (fun u -> update <- u; u)
                id
                id
                (fun s -> subscribe <- s; s)

        let (model,cmd) = init arg
        let rb = RingBuffer 10
        let mutable reentered = false
        let mutable state = model
        let rec dispatch msg =
            if reentered then
                rb.Push msg
            else
                reentered <- true
                let mutable nextMsg = Some msg
                while Option.isSome nextMsg do
                    let msg = nextMsg.Value
                    try
                        let (model',cmd') = update msg state
                        Program.setState program model' dispatch
                        cmd' |> Cmd.exec (fun ex -> Program.onError program (sprintf "Error in command while handling: %A" msg, ex)) dispatch
                        state <- model'
                    with ex ->
                        Program.onError program (sprintf "Unable to process the message: %A" msg, ex)
                    nextMsg <- rb.Pop()
                reentered <- false

        Program.setState program model dispatch
        fun () ->
            let sub =
                try
                    subscribe model
                with ex ->
                    Program.onError program ("Unable to subscribe:", ex)
                    Cmd.none
            Cmd.batch [sub; cmd]
            |> Cmd.exec (fun ex -> Program.onError program ("Error initializing:", ex)) dispatch
