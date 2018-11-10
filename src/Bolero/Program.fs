module Bolero.Program

open Elmish

/// Attach `router` to `program` when it is run as the `Program` of an `ElmishProgramComponent`.
let withRouter
        (router: IRouter<'model, 'msg>)
        (program: Program<ElmishProgramComponent<'model, 'msg>, 'model, 'msg, Node>) =
    { program with
        init = fun comp ->
            let model, initCmd = program.init comp
            let model, compCmd = comp.InitRouter(router, program, model)
            model, initCmd @ compCmd }

/// Attach a router inferred from `makeMessage` and `getEndPoint` to `program`
/// when it is run as the `Program` of an `ElmishProgramComponent`.
let withRouterInfer
        (makeMessage: 'ep -> 'msg)
        (getEndPoint: 'model -> 'ep)
        (program: Program<ElmishProgramComponent<'model, 'msg>, 'model, 'msg, Node>) =
    program
    |> withRouter (Router.infer makeMessage getEndPoint)
