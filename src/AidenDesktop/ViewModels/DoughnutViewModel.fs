module Aiden.ViewModels.DoughnutViewModel

open System
open System.Collections.ObjectModel
open Akkling
open Akka.Actor
open Elmish.Avalonia
open Data
open LiveChartsCore
open LiveChartsCore.SkiaSharpView
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
    | UpdateChartData of (string * int64) list 
    | Reset
    | SetIsFreezeChecked of bool
    | Ok
    | Terminate
type ViewModel() as self =
    // Hold onto the schedule and actorRef so we can use them later
    let mutable databaseSchedule = None : ICancelable option
    let mutable databaseActorRef = None : IActorRef option

    let startDataFetch =
        let (schedule, childActorRefGeneric) = databaseParentActor(system)
        let childActorRef = childActorRefGeneric
        databaseSchedule <- Some schedule
        databaseActorRef <- Some childActorRef

    let stopDataFetch =
        match databaseSchedule with
        | Some schedule -> schedule.Cancel()
        | None -> () 

        match databaseActorRef with
        | Some actorRef -> actorRef.Tell(Stop)
        | None -> () 

        system.Terminate() |> Async.AwaitTask |> Async.RunSynchronously

    do
        let mailboxProcessor = MailboxProcessor.Start(fun inbox ->
            let rec messageLoop () = async {
                let! msg = inbox.Receive()
                // Handle the DatabaseResponse message
                // ... (processing logic goes here)
                return! messageLoop ()
            }
            messageLoop ()
        )
        startDataFetch

    interface IDisposable with
        member _.Dispose() =
            stopDataFetch

let system = ActorSystem.Create("Aiden")
let schedule, actorRef = databaseParentActor system    
let init() =
    {
        Series = ObservableCollection<ISeries>() 
        Actions = [ { Description = "Initialized Chart"; Timestamp = DateTime.Now } ]
        IsFrozen = false
    }
let initialModel = init()

let update (msg: Msg) (model: Model) =
    match msg with
    | UpdateChartData chartData ->
        let series = chartData |> List.map (fun (vpnPart, count) ->
            PieSeries<int64>(
                Values = ObservableCollection<_>([| count |]),
                Name = vpnPart       
            ) :> ISeries
        )
        { model with
            Series = ObservableCollection<ISeries>(series)
            Actions = model.Actions @ [{ Description = "Chart data updated"; Timestamp = DateTime.Now }] 
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