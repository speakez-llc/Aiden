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

module GeoMap =
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
            VPN_Filter: string list
            TOR_Series: ObservableCollection<ISeries>
            TOR_Filter: string list
            PXY_Series: ObservableCollection<ISeries>
            PXY_Filter: string list
            MAL_Series: ObservableCollection<ISeries>
            MAL_Filter: string list
            MAL_CardData: (string * float) list
            COO_PieSeries: ObservableCollection<ISeries>
            COO_MapSeries: HeatLandSeries array
            COO_GridData: ObservableCollection<CountryData>
            COO_Filter: string list
            IsFrozen: bool
            Margin: LiveChartsCore.Measure.Margin
            currentColorSeries: Drawing.LvcColor array
            IsFetchDataForCOOChartActive: bool
            IsFreezeChecked: bool
        }

    type Msg =
        | UpdateVPNChartData of (string * int) list 
        | UpdateTORChartData of (string * int) list 
        | UpdatePXYChartData of (string * int) list 
        | UpdateMALChartData of (string * int) list 
        | UpdateCOOChartData of (string * int) list
        | UpdateCOOGridData of (string * int) list
        | SetFetchDataForCOOChartActive of bool
        | SetIsFreezeChecked of bool
        | Terminate
     

     
    let blueSeries = [|
        SKColor.Parse("#164B72").AsLvcColor(); // LightBlue
        SKColor.Parse("#0E1A45").AsLvcColor()  // DeepBlue
    |]
   
    let goldSeries = [|
        SKColor.Parse("#C18E3A").AsLvcColor(); 
        SKColor.Parse("#664B1F").AsLvcColor() 
    |]
    
    let allColorSeries = [blueSeries; goldSeries]

    let selectRandomColorSeries (currentColorSeries: Drawing.LvcColor array) =
        // Filter out the current color series
        let availableColorSeries = allColorSeries |> List.filter (fun series -> series <> currentColorSeries)
        // Select a random color series from the available ones
        let rnd = Random()
        let index = rnd.Next(availableColorSeries.Length)
        availableColorSeries[index]
        
        
    let andClauseBuilder (model: Model) =
        let vpnFilter = model.VPN_Filter
        let torFilter = model.TOR_Filter
        let pxyFilter = model.PXY_Filter
        let cooFilter = model.COO_Filter
        let malFilter = model.MAL_Filter

        let allFilters = [("vpn", vpnFilter); ("tor", torFilter); ("proxy", pxyFilter); ("cc", cooFilter); ("malware", malFilter)]

        // For each filter list, create a string that represents the AND clause for that filter
        let andClauses = allFilters |> List.map (fun (column, filter) ->
            sprintf "AND %s IN ('%s')" column (String.Join("', '", filter))
        )
        // Join all the AND clause strings with the AND operator
        String.Join(" ", andClauses)
        
    let fetchDataAsync(column: string) (model: Model) =
        async {
            // Connect to the database and execute the query
            use connection = new NpgsqlConnection(connectionString)
            do! connection.OpenAsync() |> Async.AwaitTask
            
            // Get the AND clauses from the model
            let andClauses = andClauseBuilder model
            
            // Construct the query string with the column name
            let query =
                $@"SELECT UNNEST(string_to_array({column}, ':')) AS label, COUNT(*) AS count
                   FROM events_hourly
                   WHERE event_time >= now() AT TIME ZONE 'UTC' - INTERVAL '1 minute'
                   {andClauses}
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

    let fetchDataForPXYChart model : unit -> Cmd<Msg> =
        fun () ->
            let timer = new System.Timers.Timer(rnd.Next(2998, 3002))
            timer.Elapsed.Add(fun _ ->
                async {
                    let! fetchedData = fetchDataAsync("proxy") model
                    Cmd.ofMsg (UpdatePXYChartData fetchedData) |> ignore
                } |> Async.Start
            )
            timer.Start()
            Cmd.none
    let fetchDataForMALChart model : unit -> Cmd<Msg> =
        fun () ->
            let timer = new Timer(rnd.Next(2998, 3002)) 
            timer.Elapsed.Subscribe(fun _ -> 
                async {
                    let! fetchedData = fetchDataAsync("malware") model
                    Cmd.ofMsg (UpdateMALChartData fetchedData) |> ignore
                } |> Async.Start
            ) |> ignore
            //printfn $"{DateTime.Now} MAL Subscription started"
            timer.Start()
            Cmd.none
    let fetchDataForCOOChart model : unit -> Cmd<Msg>  =
        fun () ->
            let timer = new Timer(rnd.Next(2998, 3002)) 
            timer.Elapsed.Subscribe(fun _ ->
                async {
                    let! fetchedData = fetchDataAsync("cc") model
                    Cmd.ofMsg (UpdateCOOChartData fetchedData) |> ignore
                } |> Async.Start
            ) |> ignore
            timer.Start()
            Cmd.none
    let fetchDataForVPNChart model : unit -> Cmd<Msg>  =
        fun () -> 
            let timer = new Timer(rnd.Next(2998, 3002)) 
            timer.Elapsed.Subscribe(fun _ -> 
                async {
                    let! fetchedData = fetchDataAsync("vpn") model
                    Cmd.ofMsg (UpdateVPNChartData fetchedData) |> ignore
                } |> Async.Start
            ) |> ignore
            //printfn $"{DateTime.Now} VPN Subscription started"
            timer.Start()
            Cmd.none
    let fetchDataForTORChart model : unit -> Cmd<Msg>  =
        fun () ->
            let timer = new Timer(rnd.Next(2998, 3002)) 
            timer.Elapsed.Subscribe(fun _ -> 
                async {
                    let! fetchedData = fetchDataAsync("tor") model
                    Cmd.ofMsg (UpdateTORChartData fetchedData) |> ignore
                } |> Async.Start
            ) |> ignore
            //printfn $"{DateTime.Now} TOR Subscription started"
            timer.Start()
            Cmd.none
    let fetchPieDataAsync (dataType: string) (model: Model) =
        async {
            let! data = fetchDataAsync(dataType) model
            let series = data |> List.map (fun (name, value) -> PieSeries<int>(Values = ObservableCollection<_>([| value |]), InnerRadius = 40.0, Name = name) :> ISeries)
            return ObservableCollection<ISeries>(series)
        }
    let fetchCOOGridDataAsync (dataType: string) (model: Model) =
        async {
            let! data = fetchDataAsync(dataType) model
            let updatedGridData = data |> List.map (fun (name, count) -> { Name = name.ToUpper(); Count = count }) |> List.sortBy (fun data -> data.Count) |> List.rev
            return ObservableCollection<_>(updatedGridData)
        }
        
    let fetchMALCardDataAsync (dataType: string) (model: Model) =
        async {
            let! data = fetchDataAsync(dataType) model
            let updatedCardData = data |> List.map (fun (name, count) -> (name.ToUpper(), float count))
            return updatedCardData
        }
        
    let updateCOOGridData chartData model =
        let updatedGridData =
            chartData
            |> List.map (fun (name: string, count) -> { Name = name.ToUpper(); Count = count })
            |> List.sortBy (fun data -> data.Count) |> List.rev
        { model with COO_GridData = ObservableCollection<_>(updatedGridData) }
        
    let init() (model: Model) =
        async {
            let! vpnSeries = fetchPieDataAsync("vpn") model
            let! torSeries = fetchPieDataAsync("tor") model
            let! pxySeries = fetchPieDataAsync("proxy") model
            let! malSeries = fetchPieDataAsync("malware") model
            let! malCardData = fetchMALCardDataAsync("malware") model
            let! cooSeries = fetchPieDataAsync("cc") model
            let! cooGridData = fetchCOOGridDataAsync("cc") model

            return {
                VPN_Series = vpnSeries
                VPN_Filter = [ "nord"; "foxyproxy"; "purevpn"; "surfshark"; "proton" ] 
                TOR_Series = torSeries
                TOR_Filter = [ "nord"; "foxyproxy"; "purevpn"; "surfshark"; "proton" ]
                PXY_Series = pxySeries
                PXY_Filter = [ "nord"; "foxyproxy"; "purevpn"; "surfshark"; "proton" ]
                COO_MapSeries = [| HeatLandSeries(HeatMap = blueSeries, Lands = [|
                            HeatLand(Name = "usa", Value = 47.0) :> IWeigthedMapLand
                            HeatLand(Name = "can", Value = 22.0) :> IWeigthedMapLand
                            HeatLand(Name = "ind", Value = 18.0) :> IWeigthedMapLand
                            HeatLand(Name = "kor", Value = 10.0) :> IWeigthedMapLand
                            HeatLand(Name = "egy", Value = 7.0) :> IWeigthedMapLand
                            HeatLand(Name = "rus", Value = 7.0) :> IWeigthedMapLand
                            HeatLand(Name = "gbr", Value = 6.0) :> IWeigthedMapLand
                            HeatLand(Name = "ukr", Value = 6.0) :> IWeigthedMapLand
                            HeatLand(Name = "idn", Value = 5.0) :> IWeigthedMapLand
                            HeatLand(Name = "deu", Value = 2.0) :> IWeigthedMapLand
                        |]) |]
                COO_PieSeries = cooSeries
                COO_GridData = cooGridData
                COO_Filter = [ "usa"; "can"; "ind"; "kor"; "egy"; "rus"; "gbr"; "ukr"; "idn"; "deu" ]
                MAL_CardData = malCardData 
                MAL_Series = malSeries
                MAL_Filter = [ "TRUE"; "FALSE"; "UNKNOWN" ]
                IsFrozen = false
                Margin = LiveChartsCore.Measure.Margin(50f, 50f, 50f, 50f)
                currentColorSeries = blueSeries
                IsFetchDataForCOOChartActive = true
                IsFreezeChecked = false
            }
        } |> Async.RunSynchronously


    let rec update (msg: Msg) model =
        match msg with
        | SetIsFreezeChecked isChecked ->
            { model with 
                IsFreezeChecked = isChecked
            }
        | SetFetchDataForCOOChartActive isActive ->
            { model with IsFetchDataForCOOChartActive = isActive }
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
                        landsMap.Add(nameLower, HeatLand(Name = name, Value = float value) :> IWeigthedMapLand)
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
                updateCOOGridData chartData updatedModel
            | _ ->
                // Handle case where COO_MapSeries is not initialized or in an unexpected state
                model
        | UpdateCOOGridData chartData ->
            updateCOOGridData chartData model
        | Terminate -> model

    let subscriptions (model: Model) : Cmd<Msg> =
        if not model.IsFreezeChecked then
            Cmd.batch [
                fetchDataForPXYChart model ()
                fetchDataForMALChart model ()
                fetchDataForVPNChart model ()
                fetchDataForTORChart model ()
                fetchDataForCOOChart model ()
                if model.IsFetchDataForCOOChartActive then
                    fetchDataForCOOChart model ()
            ]
        else
            Cmd.none
        
open GeoMap

type GeoMapViewModel() as this =
    inherit ReactiveElmishViewModel()
    
    let mutable dispatch : Msg -> unit = ignore

    let app = App.app

    let local = 
        Program.mkAvaloniaSimple init update
        |> Program.withErrorHandler (fun (_, ex) -> printfn "Error: %s" ex.Message)
        //|> Program.withConsoleTrace
        |> Program.withSubscription subscriptions
        // |> Program.mkStore
        //Terminate all Elmish subscriptions on dispose (view is registered as Transient).
        |> Program.mkStoreWithTerminate this Terminate
    
    
    
    member this.IsFreezeChecked 
        with get () = this.Bind (local, (fun model -> _.IsFreezeChecked))
        and set value = local.Dispatch (SetIsFreezeChecked value)
    member this.Dispatch with get() = dispatch
    member this.Margin = this.Bind(local, (fun model -> _.Margin))
    member this.VPN_Series = this.Bind(local, (fun model -> _.VPN_Series))
    member this.TOR_Series =  this.Bind(local, (fun model -> _.TOR_Series))
    member this.PXY_Series =  this.Bind(local, (fun model -> _.PXY_Series))
    member this.COO_MapSeries =  this.Bind(local, (fun model -> _.COO_MapSeries))
    member this.COO_PieSeries =  this.Bind(local, (fun model -> _.COO_PieSeries))
    member this.COO_GridData = this.Bind(local, (fun model -> _.COO_GridData))
    member this.MAL_Series =  this.Bind(local, (fun model -> _.MAL_Series))
    member this.Ok() = app.Dispatch App.GoHome
    static member DesignVM = new GeoMapViewModel()