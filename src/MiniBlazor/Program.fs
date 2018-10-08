module MiniBlazor.Program

open Elmish

let withRouter
        (router: IRouter<'model, 'msg>)
        (program: Program<ElmishProgramComponent<'model, 'msg>, 'model, 'msg, Node>) =
    { program with
        init = fun comp ->
            let model, initCmd = program.init comp
            let model, compCmd = comp.InitRouter(router, program, model)
            model, initCmd @ compCmd }

let withRouterInfer
        (makeMessage: 'ep -> 'msg)
        (getFromModel: 'model -> 'ep)
        (program: Program<ElmishProgramComponent<'model, 'msg>, 'model, 'msg, Node>) =
    program
    |> withRouter (Router.infer makeMessage getFromModel)
