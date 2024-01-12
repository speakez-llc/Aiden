namespace AidenDesktop.ViewModels

open System
open System.IO
open System.Collections.Generic
open System.Collections.ObjectModel
open System.Reactive.Linq
open System.Text.Json
open Elmish
open ReactiveElmish
open ReactiveElmish.Avalonia
open DynamicData
open SkiaSharp
open LiveChartsCore
open LiveChartsCore.Kernel.Sketches
open LiveChartsCore.SkiaSharpView
open LiveChartsCore.Defaults
open LiveChartsCore.SkiaSharpView.Painting
open Npgsql

module Chart =
    
    type EventsData =
        {
            EventTime: DateTime
            SrcIp: string
            SrcPort: int
            DstIp: string
            DstPort: int
            Cc: string
            Vpn: string
            Proxy: string
            Tor: string
            Malware: string
        }
        
    type Model = 
        {
            Series: ObservableCollection<ISeries>
            Events: ObservableCollection<EventsData>
            IsFreezeChecked: bool
        }

    type Msg =
       | UpdateSeries
       | UpdateDataGrid
       | Terminate
       | SetIsFreezeChecked of bool
    

    let rnd = Random()
    
    let connectionString =
        let filePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json")
        if File.Exists(filePath) then
            let json = JsonDocument.Parse(File.ReadAllText(filePath))
            let dbSection = json.RootElement.GetProperty("Database")
            let connectionString = dbSection.GetProperty("ConnectionString").GetString()
            connectionString
        else
            printfn $"Error: File not found: %s{filePath}"
            ""
            
     
    let fetchEventsPerSecondAsync() =
        async {
            use connection = new NpgsqlConnection(connectionString)
            do! connection.OpenAsync() |> Async.AwaitTask
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
            use connection = new NpgsqlConnection(connectionString)
            do! connection.OpenAsync() |> Async.AwaitTask
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
        
    let fetchEventsAsync() =
        async {
            try
                use connection = new NpgsqlConnection(connectionString)
                do! connection.OpenAsync() |> Async.AwaitTask
                let query =
                    $@"SELECT event_time, src_ip::text, src_port, dst_ip::text, dst_port, upper(cc) as cc, vpn, proxy, tor, malware
                        FROM events_hourly
                        WHERE event_time >= now() AT TIME ZONE 'UTC' - INTERVAL '1 minute'
                        ORDER BY event_time DESC;"

                use cmd = new NpgsqlCommand(query, connection)

                do! cmd.PrepareAsync() |> Async.AwaitTask

                let! dbReader = cmd.ExecuteReaderAsync() |> Async.AwaitTask
                use reader = dbReader
                let results =
                    [ while reader.Read() do
                        yield {
                            EventTime = reader.GetDateTime(reader.GetOrdinal("event_time"))
                            SrcIp =  reader.GetString(reader.GetOrdinal("src_ip"))
                            SrcPort = reader.GetInt32(reader.GetOrdinal("src_port"))
                            DstIp = reader.GetString(reader.GetOrdinal("dst_ip"))
                            DstPort = reader.GetInt32(reader.GetOrdinal("dst_port"))
                            Cc = if reader.GetString(reader.GetOrdinal("cc")) = "BLANK" then "" else reader.GetString(reader.GetOrdinal("cc"))
                            Vpn = if reader.GetString(reader.GetOrdinal("vpn")) = "BLANK" then "" else reader.GetString(reader.GetOrdinal("vpn"))
                            Proxy = if reader.GetString(reader.GetOrdinal("proxy")) = "BLANK" then "" else reader.GetString(reader.GetOrdinal("proxy"))
                            Tor = if reader.GetString(reader.GetOrdinal("tor")) = "BLANK" then "" else reader.GetString(reader.GetOrdinal("tor"))
                            Malware = if reader.GetString(reader.GetOrdinal("malware")) = "FALSE" then "" else reader.GetString(reader.GetOrdinal("malware"))
                        } ]
                return results |> List.sortByDescending (fun event -> event.EventTime)
            with
            | ex ->
                printfn $"Error in fetchEventsAsync: %s{ex.Message}"
                return []
        }

    let XAxes : IEnumerable<ICartesianAxis> =
        [| Axis (               
                Labeler = (fun value -> 
                    let eventTime = DateTime(int64 value)
                    let timeAgo = DateTime.UtcNow - eventTime
                    if timeAgo.TotalSeconds < 60.0 then
                        $"{timeAgo.TotalSeconds:F0} seconds ago"
                    elif timeAgo.TotalMinutes < 60.0 then
                        $"{timeAgo.TotalMinutes:F0} minutes ago"
                    else
                        $"{timeAgo.TotalHours:F0} hours ago"),
                LabelsRotation = 10,
                UnitWidth = float(TimeSpan.FromSeconds(1).Ticks),
                MinStep = float(TimeSpan.FromSeconds(1).Ticks),
                NamePaint = new SolidColorPaint(SKColors.Tan),
                LabelsPaint = new SolidColorPaint(SKColors.Tan),
                TextSize = 12.0
           )
        |]
        
    let YAxes : IEnumerable<ICartesianAxis> =
        [| Axis (
                Name = "Events per Second",
                Labeler = (fun value -> $"{value:F0}"),
                MinStep = 1.0,
                MinLimit = -0.5, 
                NamePaint = new SolidColorPaint(SKColors.Tan),
                LabelsPaint = new SolidColorPaint(SKColors.Tan),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#808080"))
            )
        |]
        
    
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
            
        let events =
            fetchEventsAsync()
            |> Async.RunSynchronously
            //|> SourceList.createFrom
            |> ObservableCollection<_>

        {
            Series = ObservableCollection<ISeries>
                [
                    LineSeries<DateTimePoint>(Values = eventsPerSecond,
                                              Name = "Total Events",
                                              GeometryFill = null,
                                              GeometryStroke = null,
                                              Stroke = new SolidColorPaint(SKColors.LightSlateGray, StrokeThickness = 4.0f)
                                              ) :> ISeries
                    LineSeries<DateTimePoint>(Values = malEventsPerSecond,
                                              Name = "Possible Threat",
                                              GeometryFill = null,
                                              GeometryStroke = null,
                                              Stroke = new SolidColorPaint(SKColor.FromHsv(30.0f, 100.0f, 100.0f, byte 190), StrokeThickness = 4.0f)
                                              ) :> ISeries
                ]
            Events = events
            IsFreezeChecked = false
        }


    let update (msg: Msg) (model: Model) =
        match msg with
        | SetIsFreezeChecked isChecked ->
            { model with 
                IsFreezeChecked = isChecked
            }
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

            let cutoff = DateTime.UtcNow.AddSeconds(-60.0)
            let values1 = model.Series[0].Values :?> ObservableCollection<DateTimePoint>
            let values2 = model.Series[1].Values :?> ObservableCollection<DateTimePoint>
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

            // Find the elements to remove
            let oldValues1 = values1 |> Seq.filter (fun point -> point.DateTime < cutoff) |> Seq.toList
            let oldValues2 = values2 |> Seq.filter (fun point -> point.DateTime < cutoff) |> Seq.toList

            // Remove the old elements
            oldValues1 |> List.iter (fun point -> ignore (values1.Remove point))
            oldValues2 |> List.iter (fun point -> ignore (values2.Remove point))
            model
        | UpdateDataGrid ->
            let newEvents = fetchEventsAsync() |> Async.RunSynchronously |> List.toSeq
            model.Events.Clear()
            model.Events.AddRange(newEvents)
            model
        | Terminate ->
            model

    let subscriptions (model: Model) : Sub<Msg> =
        let autoUpdateSub (dispatch: Msg -> unit) = 
            Observable
                .Interval(TimeSpan.FromMilliseconds(250))
                .Subscribe(fun _ ->
                    dispatch UpdateSeries
                    dispatch UpdateDataGrid
                )
        [
            if model.IsFreezeChecked = false then
                [ nameof autoUpdateSub ], autoUpdateSub
        ]

open Chart

type TimelineViewModel() as this =
    inherit ReactiveElmishViewModel()

    let local = 
        Program.mkAvaloniaSimple init update
        |> Program.withErrorHandler (fun (_, ex) -> printfn $"Error: %s{ex.Message}")
        //|> Program.withConsoleTrace
        |> Program.withSubscription subscriptions
        //|> Program.mkStore
        //Terminate all Elmish subscriptions on dispose (view is registered as Transient).
        |> Program.mkStoreWithTerminate this Terminate

    member this.Series = local.Model.Series
    
    member this.Events = local.Model.Events
    
    member this.IsFreezeChecked 
        with get () = this.Bind (local, _.IsFreezeChecked)
        and set value = local.Dispatch (SetIsFreezeChecked value)

    member this.XAxes = this.Bind (local, fun _ -> XAxes)

    member this.YAxes = this.Bind (local, fun _ -> YAxes)

    static member DesignVM = new TimelineViewModel()