module Aiden.ViewModels.DoughnutViewModel

open System
open System.Collections.ObjectModel
open Elmish.Avalonia
open Akkling
open Data
open LiveChartsCore
open LiveChartsCore.SkiaSharpView
open Messaging

type ViewModel() as self =
    // Reference to the actor from Data.fs
    let schedule, actorRef = databaseParentActor system

    // Method to send a message to start the data fetching
    let startDataFetch() =
        actorRef <! FetchData

    // Method to stop the data fetching and terminate the actor system
    let stopDataFetch() =
        schedule.Cancel() // Cancel the scheduled messages
        actorRef <! Terminate
        system.Terminate() |> Async.AwaitTask |> Async.RunSynchronously

    do
        // Start data fetching when ViewModel is created
        startDataFetch()

    interface IDisposable with
        member _.Dispose() =
            // Stop data fetching when ViewModel is disposed
            stopDataFetch()

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
    | Ok
    | NewChartData of ObservableCollection<ISeries>
    | Terminate
    
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
let initialModel = init()
let mailboxProcessor = MailboxProcessor.Start(fun inbox -> 
    let rec loop (model: Model) = async {
        let! msg = inbox.Receive()
        match msg with
        | NewChartData series ->
            // Dispatch to Elmish update function
            // Note: You'll need to ensure this is done on the UI thread if required
            ()
        | _ -> return! loop model
    }
    loop initialModel
)
let update (msg: Msg) (model: Model) =
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

let vm = 
    AvaloniaProgram.mkSimple init update bindings
    |> ElmishViewModel.create
    |> ElmishViewModel.terminateOnViewUnloaded Terminate