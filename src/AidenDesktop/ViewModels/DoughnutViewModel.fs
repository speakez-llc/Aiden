namespace AidenDesktop.ViewModels

open System
open System.IO
open System.Collections.ObjectModel
open System.Timers
open System.Text.Json
open ReactiveElmish
open ReactiveElmish.Avalonia
open Elmish
open LiveChartsCore
open LiveChartsCore.SkiaSharpView
open LiveChartsCore.SkiaSharpView.Drawing.Geometries
open LiveChartsCore.Geo
open SkiaSharp
open Npgsql

module Doughnut =
    let rnd = Random()
    // gets connection string from settings.json 
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
        
    type CountryData = {
        Name: string
        Count: int
        }    
        
    type Model = 
        {
            VPN_Series: ObservableCollection<ISeries>
            TOR_Series: ObservableCollection<ISeries>
            PXY_Series: ObservableCollection<ISeries>
            MAL_Series: ObservableCollection<ISeries>
            COO_PieSeries: ObservableCollection<ISeries>
            COO_MapSeries: HeatLandSeries array
            COO_GridData: ObservableCollection<CountryData>
            IsFrozen: bool
            Margin: LiveChartsCore.Measure.Margin
            currentColorSeries: Drawing.LvcColor array
        }

    type Msg =
        | UpdateVPNChartData of (string * int) list 
        | UpdateTORChartData of (string * int) list 
        | UpdatePXYChartData of (string * int) list 
        | UpdateMALChartData of (string * int) list 
        | UpdateCOOChartData of (string * int) list
        | UpdateCOOGridData of (string * int) list
        | Terminate
     
    let blueSeries = [|
        SKColor.Parse("#5e56f5").AsLvcColor(); // LightBlue
        SKColor.Parse("#2d2899").AsLvcColor(); // Blue
        SKColor.Parse("#100c52").AsLvcColor()  // DeepBlue
    |]

    let orangeSeries = [|
        SKColor.Parse("#ed6339").AsLvcColor(); 
        SKColor.Parse("#bf431d").AsLvcColor(); 
        SKColor.Parse("#ad2a02").AsLvcColor() 
    |]

    let greenSeries = [|
        SKColor.Parse("#47cc47").AsLvcColor(); 
        SKColor.Parse("#0cab0c").AsLvcColor(); 
        SKColor.Parse("#036603").AsLvcColor() 
    |]

    let goldSeries = [|
        SKColor.Parse("#f0d341").AsLvcColor(); 
        SKColor.Parse("#c4a81a").AsLvcColor(); 
        SKColor.Parse("#998005").AsLvcColor() 
    |]
    
    let purpleSeries = [|
        SKColor.Parse("#9e4cf5").AsLvcColor(); 
        SKColor.Parse("#671fb5").AsLvcColor(); 
        SKColor.Parse("#3c0478").AsLvcColor() 
    |]
    
    let allColorSeries = [blueSeries; orangeSeries; greenSeries; goldSeries; purpleSeries]

    let selectRandomColorSeries (currentColorSeries: Drawing.LvcColor array) =
        // Filter out the current color series
        let availableColorSeries = allColorSeries |> List.filter (fun series -> series <> currentColorSeries)
        // Select a random color series from the available ones
        let rnd = Random()
        let index = rnd.Next(availableColorSeries.Length)
        availableColorSeries.[index]
        
    let fetchDataAsync(column: string) =
        async {
            // Connect to the database and execute the query
            use connection = new NpgsqlConnection(connectionString)
            do! connection.OpenAsync() |> Async.AwaitTask
            // Construct the query string with the column name
            let query =
                $@"SELECT UNNEST(string_to_array({column}, ':')) AS label, COUNT(*) AS count
                   FROM events_hourly
                   WHERE event_time >= now() AT TIME ZONE 'UTC' - INTERVAL '1 minute'
                   GROUP BY label;"

            use cmd = new NpgsqlCommand(query, connection)

            do! cmd.PrepareAsync() |> Async.AwaitTask

            use! reader = cmd.ExecuteReaderAsync() |> Async.AwaitTask
            let results = 
                [ while reader.Read() do
                    yield (
                        reader.GetString(reader.GetOrdinal("label")),
                        reader.GetInt32(reader.GetOrdinal("count"))
                    ) ]
            return results
        }

    let fetchDataForPXYChart (dispatch: Msg -> unit) =
        let timer = new Timer(rnd.Next(2990, 3010)) // Fetch data every 5 seconds
        let disposable =
            timer.Elapsed.Subscribe(fun _ -> 
                async {
                    let! fetchedData = fetchDataAsync("proxy") 
                    dispatch (UpdatePXYChartData fetchedData)
                } |> Async.Start
            )
        //printfn $"{DateTime.Now} PXY Subscription started"
        timer.Start()
        disposable
    let fetchDataForMALChart (dispatch: Msg -> unit) =
        let timer = new Timer(rnd.Next(2990, 3010)) // Fetch data every 5 seconds
        let disposable =
            timer.Elapsed.Subscribe(fun _ -> 
                async {
                    let! fetchedData = fetchDataAsync("malware") 
                    dispatch (UpdateMALChartData fetchedData)
                } |> Async.Start
            )
        //printfn $"{DateTime.Now} MAL Subscription started"
        timer.Start()
        disposable
    let fetchDataForCOOChart (dispatch: Msg -> unit) =
        let timer = new Timer(rnd.Next(2990, 3010)) // Fetch data every 5 seconds
        let disposable =
            timer.Elapsed.Subscribe(fun _ -> 
                async {
                    let! fetchedData = fetchDataAsync("cc") 
                    dispatch (UpdateCOOChartData fetchedData)
                } |> Async.Start
            )
        //printfn $"{DateTime.Now} COO Subscription started"
        timer.Start()
        disposable  
    let fetchDataForVPNChart (dispatch: Msg -> unit) =
        let timer = new Timer(rnd.Next(2990, 3010)) // Fetch data every 5 seconds
        let disposable =
            timer.Elapsed.Subscribe(fun _ -> 
                async {
                    let! fetchedData = fetchDataAsync("vpn") 
                    dispatch (UpdateVPNChartData fetchedData)
                } |> Async.Start
            )
        //printfn $"{DateTime.Now} VPN Subscription started"
        timer.Start()
        disposable 
    let fetchDataForTORChart (dispatch: Msg -> unit) =
        let timer = new Timer(rnd.Next(2990, 3010)) // Fetch data every 5 seconds
        let disposable =
            timer.Elapsed.Subscribe(fun _ -> 
                async {
                    let! fetchedData = fetchDataAsync("tor") 
                    dispatch (UpdateTORChartData fetchedData)
                } |> Async.Start
            )
        //printfn $"{DateTime.Now} TOR Subscription started"
        timer.Start()
        disposable
      
    let fetchPieDataAsync (dataType: string) =
        async {
            let! data = fetchDataAsync(dataType)
            let series = data |> List.map (fun (name, value) -> PieSeries<int>(Values = ObservableCollection<_>([| value |]), InnerRadius = 40.0, Name = name) :> ISeries)
            return ObservableCollection<ISeries>(series)
        }
    let fetchCOOGridDataAsync (dataType: string) =
        async {
            let! data = fetchDataAsync(dataType)
            let updatedGridData = data |> List.map (fun (name, count) -> { Name = name.ToUpper(); Count = count })
            return ObservableCollection<_>(updatedGridData)
        }
        
    let init() =
        async {
            let! vpnSeries = fetchPieDataAsync("vpn")
            let! torSeries = fetchPieDataAsync("tor")
            let! pxySeries = fetchPieDataAsync("proxy")
            let! malSeries = fetchPieDataAsync("malware")
            let! cooSeries = fetchPieDataAsync("cc")
            let! cooGridData = fetchCOOGridDataAsync("cc")

            return {
                VPN_Series = vpnSeries
                TOR_Series = torSeries
                PXY_Series = pxySeries
                COO_MapSeries = [| HeatLandSeries(HeatMap = blueSeries, Lands = [|
                            HeatLand(Name = "usa", Value = 47.0) :> IWeigthedMapLand
                            HeatLand(Name = "gbr", Value = 6.0) :> IWeigthedMapLand
                            HeatLand(Name = "egy", Value = 7.0) :> IWeigthedMapLand
                            HeatLand(Name = "ind", Value = 18.0) :> IWeigthedMapLand
                            HeatLand(Name = "kor", Value = 10.0) :> IWeigthedMapLand
                            HeatLand(Name = "rus", Value = 7.0) :> IWeigthedMapLand
                            HeatLand(Name = "can", Value = 22.0) :> IWeigthedMapLand
                            HeatLand(Name = "ukr", Value = 6.0) :> IWeigthedMapLand
                            HeatLand(Name = "idn", Value = 5.0) :> IWeigthedMapLand
                            HeatLand(Name = "deu", Value = 2.0) :> IWeigthedMapLand
                        |]) |]
                COO_PieSeries = cooSeries
                COO_GridData = cooGridData
                MAL_Series = malSeries
                IsFrozen = false
                Margin = LiveChartsCore.Measure.Margin(50f, 50f, 50f, 50f)
                currentColorSeries = blueSeries 
            }

        } |> Async.RunSynchronously


    let rec update (msg: Msg) (model: Model) =
        match msg with
        | UpdateMALChartData chartData ->
            let seriesMap = 
                model.MAL_Series |> Seq.map (fun s -> (s :?> PieSeries<int>).Name, s) |> Map.ofSeq

            chartData |> List.iter (fun (name, value) ->
                match seriesMap.TryFind(name) with
                | Some series ->
                    let pieSeries = series :?> PieSeries<int>
                    pieSeries.Values <- ObservableCollection<_>([| value |]) // Assign a new collection
                | None ->
                    // Add new series
                    let newSeries = PieSeries<int>(Values = ObservableCollection<_>([| value |]), InnerRadius = 40.0, Name = name) :> ISeries
                    model.MAL_Series.Add(newSeries)
            )

            // Remove series not present in chartData
            let currentNames = chartData |> List.map fst |> Set.ofList
            model.MAL_Series
            |> Seq.toArray
            |> Array.iter (fun series ->
                let pieSeries = series :?> PieSeries<int>
                if not (Set.contains pieSeries.Name currentNames) then
                    model.MAL_Series.Remove(series) |> ignore
            )
            //printfn $"{DateTime.Now} MAL Series updated"
            model
        | UpdatePXYChartData chartData ->
            let seriesMap = 
                model.PXY_Series |> Seq.map (fun s -> (s :?> PieSeries<int>).Name, s) |> Map.ofSeq

            chartData |> List.iter (fun (name, value) ->
                match seriesMap.TryFind(name) with
                | Some series ->
                    let pieSeries = series :?> PieSeries<int>
                    pieSeries.Values <- ObservableCollection<_>([| value |]) // Assign a new collection
                | None ->
                    // Add new series
                    let newSeries = PieSeries<int>(Values = ObservableCollection<_>([| value |]), InnerRadius = 40.0, Name = name) :> ISeries
                    model.PXY_Series.Add(newSeries)
            )

            // Remove series not present in chartData
            let currentNames = chartData |> List.map fst |> Set.ofList
            model.PXY_Series
            |> Seq.toArray
            |> Array.iter (fun series ->
                let pieSeries = series :?> PieSeries<int>
                if not (Set.contains pieSeries.Name currentNames) then
                    model.PXY_Series.Remove(series) |> ignore
            )
            //printfn $"{DateTime.Now} PXY Series updated"
            model   
        | UpdateVPNChartData chartData ->
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
            //printfn $"{DateTime.Now} VPN Series updated"
            model
        | UpdateTORChartData chartData ->
            let seriesMap = 
                model.TOR_Series |> Seq.map (fun s -> (s :?> PieSeries<int>).Name, s) |> Map.ofSeq

            chartData |> List.iter (fun (name, value) ->
                match seriesMap.TryFind(name) with
                | Some series ->
                    let pieSeries = series :?> PieSeries<int>
                    pieSeries.Values <- ObservableCollection<_>([| value |]) // Assign a new collection
                | None ->
                    // Add new series
                    let newSeries = PieSeries<int>(Values = ObservableCollection<_>([| value |]), InnerRadius = 40.0, Name = name) :> ISeries
                    model.TOR_Series.Add(newSeries)
            )

            // Remove series not present in chartData
            let currentNames = chartData |> List.map fst |> Set.ofList
            model.TOR_Series
            |> Seq.toArray
            |> Array.iter (fun series ->
                let pieSeries = series :?> PieSeries<int>
                if not (Set.contains pieSeries.Name currentNames) then
                    model.TOR_Series.Remove(series) |> ignore
            )
            //printfn $"{DateTime.Now} TOR Series updated"
            model
            
        | UpdateCOOChartData chartData ->
            let seriesMap =
                model.COO_PieSeries |> Seq.map (fun s -> (s :?> PieSeries<int>).Name, s) |> Map.ofSeq

            chartData |> List.iter (fun (name, value) ->
                match seriesMap.TryFind(name) with
                | Some series ->
                    let pieSeries = series :?> PieSeries<int>
                    pieSeries.Values <- ObservableCollection<_>([| value |]) // Assign a new collection
                | None ->
                    // Add new series
                    let newSeries = PieSeries<int>(Values = ObservableCollection<_>([| value |]), InnerRadius = 40.0, Name = name) :> ISeries
                    model.COO_PieSeries.Add(newSeries)
            )

            let createHeatMap () =
                let selectedColorSeries = selectRandomColorSeries model.currentColorSeries
                selectedColorSeries
            let updateOrAddLand (series: HeatLandSeries) (name: string, value: int) =
                let nameLower = name.ToLower()
                let landsMap = series.Lands |> Seq.map (fun l -> (l.Name.ToLower(), l)) |> Map.ofSeq

                let updatedLands =
                    if Map.containsKey nameLower landsMap then
                        landsMap
                        |> Map.map (fun key l ->
                            if key = nameLower then
                                HeatLand(Name = l.Name, Value = float value) :> IWeigthedMapLand
                            else l)
                        |> Map.values
                    else
                        landsMap.Add(nameLower, new HeatLand(Name = name, Value = float value) :> IWeigthedMapLand)
                        |> Map.values
                let newHeatMap = createHeatMap()
                series.HeatMap <- newHeatMap
                series.Lands <- updatedLands |> Seq.toArray
                updatedLands, newHeatMap

            //printfn $"{DateTime.Now} COO Series updated"
            // Replace the COO_MapSeries in the model with the new series
            match model.COO_MapSeries, model.currentColorSeries with
            | [| heatLandSeries |] as existingSeries, currentColorSeries ->
                let updatedLandsAndHeatMaps = chartData |> List.map (fun (name, value) -> updateOrAddLand heatLandSeries (name, value))
                let _, newHeatMaps = List.unzip updatedLandsAndHeatMaps
                let updatedModel = { model with COO_MapSeries = existingSeries; currentColorSeries = newHeatMaps |> List.last }
                update (UpdateCOOGridData chartData) updatedModel
            | _ ->
                // Handle case where COO_MapSeries is not initialized or in an unexpected state
                model
        | UpdateCOOGridData chartData ->
            let updatedGridData = chartData |> List.map (fun (name, count) -> { Name = name.ToUpper(); Count = count })
            { model with COO_GridData = ObservableCollection<_>(updatedGridData) }
        | Terminate -> model

    let subscriptions (model: Model) : Sub<Msg> =
        [
            [ nameof fetchDataForCOOChart], fetchDataForCOOChart
            [ nameof fetchDataForMALChart], fetchDataForMALChart
            [ nameof fetchDataForVPNChart], fetchDataForVPNChart
            [ nameof fetchDataForPXYChart], fetchDataForPXYChart
            [ nameof fetchDataForTORChart], fetchDataForTORChart
        ]
        
open Doughnut

type DoughnutViewModel() as this =
    inherit ReactiveElmishViewModel()

    let app = App.app

    let local = 
        Program.mkAvaloniaSimple init update
        |> Program.withErrorHandler (fun (_, ex) -> printfn "Error: %s" ex.Message)
        //|> Program.withConsoleTrace
        |> Program.withSubscription subscriptions
        // |> Program.mkStore
        //Terminate all Elmish subscriptions on dispose (view is registered as Transient).
        |> Program.mkStoreWithTerminate this Terminate
    
    member this.Margin = this.Bind(local, _.Margin)
    member this.VPN_Series = this.Bind(local, _.VPN_Series)
    member this.TOR_Series =  this.Bind(local, _.TOR_Series)
    member this.PXY_Series =  this.Bind(local, _.PXY_Series)
    member this.COO_MapSeries =  this.Bind(local, _.COO_MapSeries)
    member this.COO_PieSeries =  this.Bind(local, _.COO_PieSeries)
    member this.COO_GridData = this.Bind(local, _.COO_GridData)
    member this.MAL_Series =  this.Bind(local, _.MAL_Series)
    member this.Ok() = app.Dispatch App.GoHome
    static member DesignVM = new DoughnutViewModel()