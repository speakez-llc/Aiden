module Aiden.ViewModels.DoughnutViewModel

open System
open System.Collections.ObjectModel
open System.Configuration
open System.Timers
open Elmish
open Elmish.Avalonia
open LiveChartsCore
open LiveChartsCore.SkiaSharpView
open Npgsql
open Messaging

// gets connection string from settings.json
let connectionString = ()
    // let settings = ConfigurationManager.AppSettings
    // settings.Get("ConnectionString")
printfn $"Connection String: %s{connectionString}"
    
type Model = 
    {
        VPN_Series: ObservableCollection<ISeries>
        IsFrozen: bool
        Margin: LiveChartsCore.Measure.Margin
    }
    
and Action = 
    {
        Description: string
        Timestamp: DateTime
    }

type Msg =
    | FetchData of (string * int) list 
    | UpdateChartData of (string * int) list 
    | SetIsFreezeChecked of bool
    | Ok
    | Terminate
    
let fetchDataAsync () =
    async {
        // Connect to the database and execute the query
        use connection = new NpgsqlConnection(connectionString)
        do! connection.OpenAsync() |> Async.AwaitTask
        let query = 
            @"SELECT UNNEST(string_to_array(vpn, ':')) AS vpn_part, COUNT(*) AS count
              FROM events_hourly
              WHERE event_time >= now() AT TIME ZONE 'UTC' - INTERVAL '1 minute'
              GROUP BY vpn_part;"

        use cmd = new NpgsqlCommand(query, connection)
        use! reader = cmd.ExecuteReaderAsync() |> Async.AwaitTask
        let results = 
            [ while reader.Read() do
                yield (
                    reader.GetString(reader.GetOrdinal("vpn_part")),
                    reader.GetInt32(reader.GetOrdinal("count"))
                ) ]
        return results
    }
    
let fetchDataForChart (dispatch: Msg -> unit) =
    let timer = new Timer(2000) // Fetch data every 2 seconds
    let disposable =
        timer.Elapsed.Subscribe(fun _ -> 
            async {
                let! fetchedData = fetchDataAsync()
                dispatch (UpdateChartData fetchedData)
            } |> Async.Start
        )
    timer.Start()
    disposable
    
let init() =
    {
        VPN_Series = ObservableCollection<ISeries>() 
        IsFrozen = false
        Margin = LiveChartsCore.Measure.Margin(50f, 50f, 50f, 50f)
    }

let update (msg: Msg) (model: Model) =
    match msg with
    | FetchData chartData ->
        model
    | UpdateChartData chartData ->
        let seriesMap = 
            model.VPN_Series |> Seq.map (fun s -> (s :?> PieSeries<int>).Name, s) |> Map.ofSeq

        chartData |> List.iter (fun (name, value) ->
            match seriesMap.TryFind(name) with
            | Some series ->
                let pieSeries = series :?> PieSeries<int>
                pieSeries.Values <- ObservableCollection<_>([| value |]) // Assign a new collection
            | None ->
                // Add new series
                let newSeries = PieSeries<int>(Values = ObservableCollection<_>([| value |]), InnerRadius = 40.0, Name = name) :> ISeries
                model.VPN_Series.Add(newSeries)
        )

        // Remove series not present in chartData
        let currentNames = chartData |> List.map fst |> Set.ofList
        model.VPN_Series
        |> Seq.toArray
        |> Array.iter (fun series ->
            let pieSeries = series :?> PieSeries<int>
            if not (Set.contains pieSeries.Name currentNames) then
                model.VPN_Series.Remove(series) |> ignore
        )
        // Update Actions
        model

    | SetIsFreezeChecked isChecked ->
        { model with 
            IsFrozen = isChecked
        }
    | Ok -> 
        bus.OnNext(GlobalMsg.GoHome)
        { model with IsFrozen = false }
    | Terminate ->
        model

let bindings ()  : Binding<Model, Msg> list = [
    "IsFrozen" |> Binding.twoWay ((fun m -> m.IsFrozen), SetIsFreezeChecked)
    "VPN_Series" |> Binding.oneWayLazy ((fun m -> m.VPN_Series), (fun _ _ -> true), id)
    "Margin" |> Binding.oneWay (fun m -> m.Margin)
    "Ok" |> Binding.cmd Ok
]

let designVM = ViewModel.designInstance (init()) (bindings())

let subscriptions (model: Model) : Sub<Msg> =
    [
        [ nameof fetchDataForChart], fetchDataForChart
    ] 

let vm = 
    AvaloniaProgram.mkSimple init update bindings
    |> AvaloniaProgram.withSubscription subscriptions
    |> ElmishViewModel.create
    |> ElmishViewModel.terminateOnViewUnloaded Terminate
