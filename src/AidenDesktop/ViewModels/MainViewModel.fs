module Aiden.ViewModels.MainViewModel

open Elmish.Avalonia
open Elmish

type Model = 
    {
        ContentVM: IElmishViewModel
    }

type Msg = 
    | ShowChart
    | ShowCounter
    | ShowDoughnut
    | ShowAbout
    | Terminate

let init() = 
    { 
        ContentVM = DoughnutViewModel.vm
    }

let update (msg: Msg) (model: Model) = 
    match msg with
    | ShowCounter -> 
        { model with ContentVM = CounterViewModel.vm }
    | ShowChart -> 
        { model with ContentVM = ChartViewModel.vm }  
    | ShowDoughnut -> 
        { model with ContentVM = DoughnutViewModel.vm }  
    | ShowAbout ->
        { model with ContentVM = AboutViewModel.vm }
    | Terminate ->
        model

let bindings() : Binding<Model, Msg> list = [   
    // Properties
    "ContentVM" |> Binding.oneWay (fun m -> m.ContentVM)
    "ShowCounter" |> Binding.cmd ShowCounter
    "ShowChart" |> Binding.cmd ShowChart
    "ShowDoughnut" |> Binding.cmd ShowDoughnut
    "ShowAbout" |> Binding.cmd ShowAbout
]

let designVM = ViewModel.designInstance (init()) (bindings())

let vm : IElmishViewModel = 
    let subscriptions (model: Model) : Sub<Msg> =
        let messageBusSub (dispatch: Msg -> unit) = 
            Messaging.bus.Subscribe(fun msg -> 
                match msg with
                | Messaging.GlobalMsg.GoHome -> 
                    dispatch ShowCounter
            )

        [ 
            [ nameof messageBusSub ], messageBusSub
        ]

    AvaloniaProgram.mkSimple init update bindings
    |> AvaloniaProgram.withSubscription subscriptions
    |> ElmishViewModel.create
    //|> ElmishViewModel.terminateOnUnloaded Terminate
    :> IElmishViewModel
