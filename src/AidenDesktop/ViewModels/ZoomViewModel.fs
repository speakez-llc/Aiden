namespace AidenDesktop.ViewModels

open System
open System.Linq
open System.IO
open System.Collections.Generic
open System.Collections.ObjectModel
open System.Reactive.Linq
open System.Text.Json
open Avalonia.Controls
open LiveChartsCore.Drawing
open LiveChartsCore.Kernel.Events
open LiveChartsCore.Measure
open LiveChartsCore.SkiaSharpView.Drawing
open LiveChartsCore.VisualElements
open ReactiveUI
open ReactiveElmish
open ReactiveElmish.Avalonia
open Elmish
open SkiaSharp
open LiveChartsCore
open LiveChartsCore.Kernel.Sketches
open LiveChartsCore.SkiaSharpView
open LiveChartsCore.Defaults
open LiveChartsCore.SkiaSharpView.Painting
open Npgsql

module Zoom =
    
    type Msg =
       | UpdateSeries
       | PointerDown
       | PointerUp
       | PointerMove of PointerCommandArgs
       | ChartUpdated of ChartCommandArgs
       | Terminate
      
    type Model = 
        {
            Series: ObservableCollection<ISeries>
            ScrollbarSeries: ObservableCollection<ISeries>
            ScrollableAxes: ObservableCollection<Axis>
            Thumbs: ObservableCollection<RectangularSection>
            InvisibleX: ObservableCollection<Axis>
            InvisibleY: ObservableCollection<Axis>
            IsDown: bool
            Margin: Margin
        }

 
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
          
    let fetchEventsPerHourAsync() =
        async {
            use connection = new NpgsqlConnection(connectionString)
            do! connection.OpenAsync() |> Async.AwaitTask
            let query =
                $@"WITH time_series AS (
                    SELECT generate_series(
                        date_trunc('minute', now() AT TIME ZONE 'UTC') - INTERVAL '1 day',
                        date_trunc('minute', now() AT TIME ZONE 'UTC'),
                        '1 hour'::interval
                    ) AS event_time
                )
                SELECT
                    time_series.event_time,
                    COALESCE(events.count, 0) AS count
                FROM
                    time_series
                LEFT JOIN (
                    SELECT
                        date_trunc('minute', event_time) AS event_time,
                        COUNT(*) AS count
                    FROM
                        events_hourly
                    WHERE
                        event_time >= now() AT TIME ZONE 'UTC' - INTERVAL '1 day'
                    GROUP BY
                        date_trunc('minute', event_time)
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
        
    let fetchMALEventsPerHourAsync() =
        async {
            use connection = new NpgsqlConnection(connectionString)
            do! connection.OpenAsync() |> Async.AwaitTask
            let query =
                $@"WITH time_series AS (
                    SELECT generate_series(
                        date_trunc('minute', now() AT TIME ZONE 'UTC') - INTERVAL '1 day',
                        date_trunc('minute', now() AT TIME ZONE 'UTC'),
                        '1 hour'::interval
                    ) AS event_time
                )
                SELECT
                    time_series.event_time,
                    COALESCE(events.count, 0) AS count
                FROM
                    time_series
                LEFT JOIN (
                    SELECT
                        date_trunc('minute', event_time) AS event_time,
                        COUNT(*) AS count
                    FROM
                        events_hourly
                    WHERE
                        event_time >= now() AT TIME ZONE 'UTC' - INTERVAL '1 day'
                        AND malware in ('TRUE')
                    GROUP BY
                        date_trunc('minute', event_time)
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
                Name = "Events per Minute",
                Labeler = (fun value -> $"{value:F0}"),
                MinStep = 1.0,
                MinLimit = 0.0,
                NamePaint = new SolidColorPaint(SKColors.Tan),
                LabelsPaint = new SolidColorPaint(SKColors.Tan),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#808080"))
            )
        |]
        
    let init() =
        let eventsPerHour = 
            fetchEventsPerHourAsync()
            |> Async.RunSynchronously
            |> List.map (fun (time, count) -> DateTimePoint(time, float count))
            |> ObservableCollection<_>

        let malEventsPerHour = 
            fetchMALEventsPerHourAsync()
            |> Async.RunSynchronously
            |> List.map (fun (time, count) -> DateTimePoint(time, float count))
            |> ObservableCollection<_>
            
        let minTime = eventsPerHour |> Seq.minBy (fun point -> point.DateTime)
        let maxTime = eventsPerHour |> Seq.maxBy (fun point -> point.DateTime)

        {
            Series = ObservableCollection<ISeries>
                [
                    LineSeries<DateTimePoint>(Values = eventsPerHour,
                                              Name = "Total Events",
                                              GeometryFill = null,
                                              GeometryStroke = null,
                                              Stroke = new SolidColorPaint(SKColors.LightSlateGray, StrokeThickness = 4.0f)
                                              ) :> ISeries
                    LineSeries<DateTimePoint>(Values = malEventsPerHour,
                                              Name = "Possible Threat",
                                              GeometryFill = null,
                                              GeometryStroke = null,
                                              Stroke = new SolidColorPaint(SKColor.FromHsv(30.0f, 100.0f, 100.0f, byte 190), StrokeThickness = 4.0f)
                                              ) :> ISeries
                ]
            ScrollbarSeries = ObservableCollection<ISeries>
                [
                    LineSeries<DateTimePoint>(Values = eventsPerHour,
                                              Name = "Total Events",
                                              GeometryFill = null,
                                              GeometryStroke = null,
                                              Stroke = new SolidColorPaint(SKColors.LightSlateGray, StrokeThickness = 4.0f)
                                              ) :> ISeries
                    LineSeries<DateTimePoint>(Values = malEventsPerHour,
                                              Name = "Possible Threat",
                                              GeometryFill = null,
                                              GeometryStroke = null,
                                              Stroke = new SolidColorPaint(SKColor.FromHsv(30.0f, 100.0f, 100.0f, byte 190), StrokeThickness = 4.0f)
                                              ) :> ISeries
                ]
            Thumbs = ObservableCollection<RectangularSection>
                [
                    RectangularSection(Xi = float minTime.DateTime.Ticks,
                                       Xj = float maxTime.DateTime.Ticks,
                                       Yi = 20.0,
                                       Yj = 120.0,
                                       Fill = new SolidColorPaint(SKColors.Aqua),
                                       Stroke = new SolidColorPaint(SKColors.Fuchsia))
                ]
            ScrollableAxes = ObservableCollection<Axis> [ Axis(
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
            ) ]
            InvisibleX = ObservableCollection<Axis> [ Axis(IsVisible = false) ]
            InvisibleY = ObservableCollection<Axis> [ Axis(IsVisible = false) ]
            IsDown = false
            Margin = Margin(100f, 0f, 50f, 50f)
        }

        
    let update (msg: Msg) (model: Model) =
        match msg with
        | PointerUp ->
            //printfn $"{DateTime.Now} pointer is up"
            { model with IsDown = false }
        | PointerDown ->
            //printfn $"{DateTime.Now} pointer is down in Elmish"
            { model with IsDown = true }
        | PointerMove args ->
            if not model.IsDown then
                model
            else
                //printfn $"{DateTime.Now} pointer is moved"
                let chart = args.Chart :?> ICartesianChartView<SkiaSharpDrawingContext>
                let positionInData = chart.ScalePixelsToData(args.PointerPosition)

                let thumb = model.Thumbs.[0]
                let currentRange = thumb.Xj.Value - thumb.Xi.Value
                // update the scroll bar thumb when the user is dragging the chart
                thumb.Xi <- positionInData.X - currentRange / 2.0
                thumb.Xj <- positionInData.X + currentRange / 2.0

                // update the chart visible range
                model.ScrollableAxes.[0].MinLimit <- thumb.Xi.Value
                model.ScrollableAxes.[0].MaxLimit <- thumb.Xj.Value

                // TODO: Duplication of code, but needed to get model to update
                let axis = Axis(
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
                    TextSize = 12.0,
                        MinLimit = thumb.Xi.Value,
                        MaxLimit = thumb.Xj.Value
                )
                model.ScrollableAxes.[0] <- axis

                // update the thumb rectangle's Xi and Xj properties
                thumb.Xi <- model.ScrollableAxes.[0].MinLimit.Value
                thumb.Xj <- model.ScrollableAxes.[0].MaxLimit.Value
                
                model

        | ChartUpdated args ->
            //printfn $"{DateTime.Now} chart updated"
            let chart = args.Chart :?> ICartesianChartView<SkiaSharpDrawingContext>
            let xAxis = (chart.XAxes.OfType<Axis>()).FirstOrDefault()
            if System.Nullable<float>.Equals(xAxis.MaxLimit, null) then
                model
            else
                let thumb = model.Thumbs.[0]
                // Update the Xi and Xj properties of the Thumb rectangle
                thumb.Xi <- xAxis.MinLimit.Value 
                thumb.Xj <- xAxis.MaxLimit.Value
                model
        | UpdateSeries ->
            let latestEvents =
                fetchEventsPerHourAsync()
                |> Async.RunSynchronously
                |> List.map (fun (time, count) -> DateTimePoint(time, float count))

            let malEventsPerSecond =
                fetchMALEventsPerHourAsync()
                |> Async.RunSynchronously
                |> List.map (fun (time, count) -> DateTimePoint(time, float count))

            // Update existing data points and add new ones

            let cutoff = DateTime.UtcNow.AddHours(-24.0)
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
        | Terminate ->
            model
            
open Zoom
open Avalonia.Threading

type ZoomViewModel() as this =
    inherit ReactiveElmishViewModel()

    let local =
        Program.mkAvaloniaSimple init update
        |> Program.withErrorHandler (fun (_, ex) -> printfn $"Error: %s{ex.Message}")
        //|> Program.withConsoleTrace
        //|> Program.withSubscription subscriptions
        //|> Program.mkStore
        //Terminate all Elmish subscriptions on dispose (view is registered as Transient).
        |> Program.mkStoreWithTerminate this Terminate

    member this.Series = local.Model.Series
    member this.ScrollbarSeries = local.Model.ScrollbarSeries
    member this.ScrollableAxes = local.Model.ScrollableAxes
    member this.InvisibleX = local.Model.InvisibleX
    member this.InvisibleY = local.Model.InvisibleY
    
    member val PointerDownCommand = ReactiveCommand.Create<obj, unit> (fun _ -> 
        //printfn $"{DateTime.Now} pointer is down"
        local.Dispatch PointerDown) with get, set
    member val PointerMoveCommand = ReactiveCommand.Create<PointerCommandArgs, unit> (fun args -> local.Dispatch (PointerMove args)) with get, set
    member val PointerUpCommand = ReactiveCommand.Create<obj, unit> (fun _ -> local.Dispatch PointerUp) with get, set
    member val ChartUpdatedCommand = ReactiveCommand.Create<ChartCommandArgs, unit> (fun args -> local.Dispatch (ChartUpdated args)) with get, set

    member this.Thumbs = local.Model.Thumbs
    member this.Margin = local.Model.Margin

    member this.XAxes = this.Bind (local, fun _ -> XAxes)

    member this.YAxes = this.Bind (local, fun _ -> YAxes)

    static member DesignVM = new ZoomViewModel()