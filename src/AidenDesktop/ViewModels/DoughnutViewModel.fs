module Aiden.ViewModels.DoughnutViewModel

open System
open Akka.Actor
open System.Collections.Generic
open System.Collections.ObjectModel
open Aiden.Services
open Elmish
open Elmish.Avalonia
open LiveChartsCore
open LiveChartsCore.Kernel.Sketches
open LiveChartsCore.SkiaSharpView
open LiveChartsCore.Defaults
open Messaging
open Data

type ViewModel() as self =
    // Instantiate the actor system
    let system = ActorSystem.Create("mySystem")

    // A method to initialize the actors
    let initializeActors() =
        // ... create your actors here ...

    // A method to shut down the actor system
    let terminateActorSystem() =
        async {
            do! system.Terminate() |> Async.AwaitTask
            return ()
        } |> Async.RunSynchronously

    do
        initializeActors()

    // Implement IDisposable if not already implemented
    interface IDisposable with
        member _.Dispose() =
            terminateActorSystem()

    // If there is a view unload event you can listen to
    member this.OnViewUnloaded() =
        terminateActorSystem()


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
    
let mailboxProcessor = MailboxProcessor.Start(fun inbox -> 
    let rec loop (model: Model) = async {
        let! msg = inbox.Receive()
        match msg with
        | NewChartData series ->
            // Dispatch to Elmish update function
            // Note: You'll need to ensure this is done on the UI thread if required
            Program.dispatch (UpdateChart series) // Assuming `UpdateChart` is a new message in Elmish that you handle to update the series
        | _ -> return! loop model
    }
    loop initialModel
)

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