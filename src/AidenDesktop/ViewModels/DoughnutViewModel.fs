module Aiden.ViewModels.DoughnutViewModel

open System
open System.Collections.Generic
open System.Collections.ObjectModel
open Elmish
open Elmish.Avalonia
open LiveChartsCore
open LiveChartsCore.Kernel.Sketches
open LiveChartsCore.SkiaSharpView
open LiveChartsCore.Defaults
open Messaging

type Model = 
    {
        Series: ObservableCollection<ISeries>
        Actions: Action list
        IsFrozen: bool
    }
    
and Action = 
    {
        Description: string
        Timestamp: DateTime
    }

type Msg = 
    | Update
    | Reset
    | SetIsFreezeChecked of bool
    | Terminate
    | Ok

let init() =
    let staticData =
        [ 2.0; 4.0; 1.0; 4.0; 3.0 ] // Static values for the chart

    let pieSeries =
        staticData
        |> Seq.mapi (fun index value -> 
            PieSeries<int>(Values = [int value], InnerRadius = 50.0)
            :> ISeries)
    {
        Series = ObservableCollection<ISeries>(pieSeries) 
        Actions = [ { Description = "Initialized Chart"; Timestamp = DateTime.Now } ]
        IsFrozen = false
    }

let update (msg: Msg) (model: Model) =
    let values = model.Series[0].Values :?> ObservableCollection<DateTimePoint>
    match msg with
    | Update ->
        { model with 
            Actions = model.Actions @ [ { Description = $"Updated Item:"; Timestamp = DateTime.Now } ]            
        }
    | Reset ->
        // insert new Series - send the current series length to the newSeries function
        
        { model with
            // deactivate the AutoUpdate ToggleButton in the UI
            IsFrozen = false 
            Actions = [ { Description = "Reset Chart"; Timestamp = DateTime.Now } ]
        }
    | SetIsFreezeChecked isChecked ->
        { model with 
            IsFrozen = isChecked
            Actions = model.Actions @ [ { Description = $"Is Freeze Checked: {isChecked}"; Timestamp = DateTime.Now } ]
        }
    | Ok -> 
        bus.OnNext(GlobalMsg.GoHome)
        { model with IsFrozen = false }
    | Terminate ->
        model

let bindings ()  : Binding<Model, Msg> list = [
    "Actions" |> Binding.oneWay (fun m -> List.rev m.Actions)
    "Reset" |> Binding.cmd Reset
    "IsFrozen" |> Binding.twoWay ((fun m -> m.IsFrozen), SetIsFreezeChecked)
    "Series" |> Binding.oneWayLazy ((fun m -> m.Series), (fun _ _ -> true), id)
    "Ok" |> Binding.cmd Ok
]

let designVM = ViewModel.designInstance (init()) (bindings())

open System.Timers



let vm = 
    AvaloniaProgram.mkSimple init update bindings
    |> ElmishViewModel.create
    |> ElmishViewModel.terminateOnViewUnloaded Terminate