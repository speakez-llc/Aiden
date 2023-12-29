namespace AidenDesktop.ViewModels

open System
open System.Data
open System.IO
open System.Collections.Generic
open System.Collections.ObjectModel
open System.Reactive.Linq
open System.Text.Json
open Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings
open LiveChartsCore.SkiaSharpView.Drawing.Geometries
open ReactiveElmish
open ReactiveElmish.Avalonia
open Elmish
open DynamicData
open LiveChartsCore
open LiveChartsCore.Kernel.Sketches
open LiveChartsCore.SkiaSharpView
open LiveChartsCore.Defaults
open Npgsql

module Chart = 

    let rnd = Random()
    
    let connectionString =
        let filePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json")
        if File.Exists(filePath) then
            let json = JsonDocument.Parse(File.ReadAllText(filePath))
            let dbSection = json.RootElement.GetProperty("Database")
            let connectionString = dbSection.GetProperty("ConnectionString").GetString()
            //printfn $"Connection String: %s{connectionString}"
            connectionString
        else
            printfn $"Error: File not found: %s{filePath}"
            ""
            
    let fetchEventsAsync() =
        async {
            // Connect to the database and execute the query
            use connection = new NpgsqlConnection(connectionString)
            do! connection.OpenAsync() |> Async.AwaitTask
            // Construct the query string with the column name
            let query =
                $@"SELECT event_time, src_ip, src_port, dst_ip, dst_port, cc, vpn, proxy, tor, malware  
                   FROM events_hourly
                   WHERE event_time >= now() AT TIME ZONE 'UTC' - INTERVAL '1 minute'
                   ORDER BY event_time DESC;"

            use cmd = new NpgsqlCommand(query, connection)

            do! cmd.PrepareAsync() |> Async.AwaitTask

            use! reader = cmd.ExecuteReaderAsync() |> Async.AwaitTask
            let results = 
                [ while reader.Read() do
                    yield (
                        reader.GetDateTime(reader.GetOrdinal("event_time")),
                        reader.GetString(reader.GetOrdinal("src_ip")),
                        reader.GetInt32(reader.GetOrdinal("src_port")),
                        reader.GetString(reader.GetOrdinal("dst_ip")),
                        reader.GetInt32(reader.GetOrdinal("dst_port")),
                        reader.GetString(reader.GetOrdinal("cc")),
                        reader.GetString(reader.GetOrdinal("vpn")),
                        reader.GetString(reader.GetOrdinal("proxy")),
                        reader.GetString(reader.GetOrdinal("tor")),
                        reader.GetString(reader.GetOrdinal("malware"))
                    ) ]
            return results
        }
        
    let fetchEventsPerSecondAsync() =
        async {
            // Connect to the database and execute the query
            use connection = new NpgsqlConnection(connectionString)
            do! connection.OpenAsync() |> Async.AwaitTask
            // Construct the query string with the column name
            let query =
                $@"WITH time_series AS (
                    SELECT generate_series(
                        date_trunc('second', now() AT TIME ZONE 'UTC') - INTERVAL '1 minute',
                        date_trunc('second', now() AT TIME ZONE 'UTC'),
                        '1 second'::interval
                    ) AS event_time
                )
                SELECT
                    time_series.event_time,
                    COALESCE(events.count, 0) AS count
                FROM
                    time_series
                LEFT JOIN (
                    SELECT
                        date_trunc('second', event_time) AS event_time,
                        COUNT(*) AS count
                    FROM
                        events_hourly
                    WHERE
                        event_time >= now() AT TIME ZONE 'UTC' - INTERVAL '1 minute'
                    GROUP BY
                        date_trunc('second', event_time)
                ) AS events
                ON time_series.event_time = events.event_time
                    ORDER BY
                        time_series.event_time ASC;"

            use cmd = new NpgsqlCommand(query, connection)

            do! cmd.PrepareAsync() |> Async.AwaitTask

            use! reader = cmd.ExecuteReaderAsync() |> Async.AwaitTask
            let results =
                [ while reader.Read() do
                    yield (
                        reader.GetDateTime(reader.GetOrdinal("event_time")),
                        reader.GetInt32(reader.GetOrdinal("count"))
                    ) ]
            return results
        }
        
    let fetchMALEventsPerSecondAsync() =
        async {
            // Connect to the database and execute the query
            use connection = new NpgsqlConnection(connectionString)
            do! connection.OpenAsync() |> Async.AwaitTask
            // Construct the query string with the column name
            let query =
                $@"WITH time_series AS (
                    SELECT generate_series(
                        date_trunc('second', now() AT TIME ZONE 'UTC') - INTERVAL '1 minute',
                        date_trunc('second', now() AT TIME ZONE 'UTC'),
                        '1 second'::interval
                    ) AS event_time
                )
                SELECT
                    time_series.event_time,
                    COALESCE(events.count, 0) AS count
                FROM
                    time_series
                LEFT JOIN (
                    SELECT
                        date_trunc('second', event_time) AS event_time,
                        COUNT(*) AS count
                    FROM
                        events_hourly
                    WHERE
                        event_time >= now() AT TIME ZONE 'UTC' - INTERVAL '1 minute'
                        AND malware in ('TRUE')
                    GROUP BY
                        date_trunc('second', event_time)
                ) AS events
                ON time_series.event_time = events.event_time
                    ORDER BY
                        time_series.event_time ASC;"

            use cmd = new NpgsqlCommand(query, connection)

            do! cmd.PrepareAsync() |> Async.AwaitTask

            use! reader = cmd.ExecuteReaderAsync() |> Async.AwaitTask
            let results =
                [ while reader.Read() do
                    yield (
                        reader.GetDateTime(reader.GetOrdinal("event_time")),
                        reader.GetInt32(reader.GetOrdinal("count"))
                    ) ]
            return results
        }

    
    
    // create time labeling for the X axis in the Chart visual
    let XAxes : IEnumerable<ICartesianAxis> =
        [| Axis (
                Labeler = (fun value -> DateTime(int64 value).ToString("HH:mm:ss")),
                LabelsRotation = 15,
                UnitWidth = float(TimeSpan.FromSeconds(1).Ticks),
                MinStep = float(TimeSpan.FromSeconds(1).Ticks)
            )
        |]

    type Model = 
        {
            Series: ObservableCollection<ISeries>
        }


    type Msg =
       | RemoveOldSeries
       | UpdateSeries
       | Terminate
    
    let init() =
        let eventsPerSecond = 
            fetchEventsPerSecondAsync()
            |> Async.RunSynchronously
            |> List.map (fun (time, count) -> DateTimePoint(time, float count))
            |> ObservableCollection<_>

        let malEventsPerSecond = 
            fetchMALEventsPerSecondAsync()
            |> Async.RunSynchronously
            |> List.map (fun (time, count) -> DateTimePoint(time, float count))
            |> ObservableCollection<_>

        {
            Series = ObservableCollection<ISeries>
                [
                    LineSeries<DateTimePoint>(Values = eventsPerSecond, Name = "Events per Second", GeometryFill = null, GeometryStroke = null) :> ISeries
                    ColumnSeries<DateTimePoint>(Values = malEventsPerSecond, Name = "MAL Events/Sec") :> ISeries
                ]
        }

    let update (msg: Msg) (model: Model) =
        match msg with
        | RemoveOldSeries ->
            let cutoff = DateTime.UtcNow.AddSeconds(-60.0)
            let values1 = model.Series.[0].Values :?> ObservableCollection<DateTimePoint>
            let values2 = model.Series.[1].Values :?> ObservableCollection<DateTimePoint>

            // Find the elements to remove
            let oldValues1 = values1 |> Seq.filter (fun point -> point.DateTime < cutoff) |> Seq.toList
            let oldValues2 = values2 |> Seq.filter (fun point -> point.DateTime < cutoff) |> Seq.toList

            // Remove the old elements
            oldValues1 |> List.iter (fun point -> ignore (values1.Remove point))
            oldValues2 |> List.iter (fun point -> ignore (values2.Remove point))

            model
        | UpdateSeries ->
            let latestEvents =
                fetchEventsPerSecondAsync()
                |> Async.RunSynchronously
                |> List.map (fun (time, count) -> DateTimePoint(time, float count))

            let malEventsPerSecond =
                fetchMALEventsPerSecondAsync()
                |> Async.RunSynchronously
                |> List.map (fun (time, count) -> DateTimePoint(time, float count))

            // Update existing data points and add new ones
            let values1 = model.Series.[0].Values :?> ObservableCollection<DateTimePoint>
            let values2 = model.Series.[1].Values :?> ObservableCollection<DateTimePoint>
            latestEvents
            |> List.iter (fun point ->
                match Seq.tryFind (fun (p: DateTimePoint) -> p.DateTime = point.DateTime) values1 with
                | Some existingPoint -> existingPoint.Value <- point.Value
                | None -> values1.Insert(values1.Count, point))

            malEventsPerSecond
            |> List.iter (fun point ->
                match Seq.tryFind (fun (p: DateTimePoint) -> p.DateTime = point.DateTime) values2 with
                | Some existingPoint -> existingPoint.Value <- point.Value
                | None -> values2.Insert(values2.Count, point))

            model
        | Terminate ->
            model

    let subscriptions (model: Model) : Sub<Msg> =
        let autoUpdateSub (dispatch: Msg -> unit) = 
            Observable
                .Interval(TimeSpan.FromSeconds(1))
                .Subscribe(fun _ ->
                    dispatch UpdateSeries
                    dispatch RemoveOldSeries
                )
        [
            [ nameof autoUpdateSub ], autoUpdateSub
        ]

open Chart

type ChartViewModel() as this =
    inherit ReactiveElmishViewModel()

    let app = App.app

    let local = 
        Program.mkAvaloniaSimple init update
        |> Program.withErrorHandler (fun (_, ex) -> printfn "Error: %s" ex.Message)
        //|> Program.withConsoleTrace
        |> Program.withSubscription subscriptions
        //|> Program.mkStore
        //Terminate all Elmish subscriptions on dispose (view is registered as Transient).
        |> Program.mkStoreWithTerminate this Terminate 

    member this.Series = local.Model.Series

    member this.XAxes = this.Bind (local, fun _ -> XAxes)


    static member DesignVM = new ChartViewModel()