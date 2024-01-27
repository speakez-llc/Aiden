namespace AidenDesktop.ViewModels

open System
open System.IO
open System.Timers
open System.Text.Json
open System.Collections.ObjectModel
open ReactiveElmish
open ReactiveElmish.Avalonia
open Elmish
open AidenDesktop.Models
open App
open Npgsql


module Dashboard =

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
    
    type Model =
        {
            IsLoading : bool
            IsFrozen : bool
            TimeFrame : string
            Panels : DragPanel List
            VPNSeries : ObservableCollection<SeriesData>
            TORSeries : ObservableCollection<SeriesData>
            PRXSeries : ObservableCollection<SeriesData>
            COOSeries : ObservableCollection<SeriesData>
            MALSeries : ObservableCollection<SeriesData>

            VPNFilter : FilterItem List
            TORFilter : FilterItem List
            PRXFilter : FilterItem List
            COOFilter : FilterItem List
            MALFilter : FilterItem List

            IsDragging : bool

        }
    
    type Msg =
        | OpenPanel of String
        | ClosePanel of int
        | SetPanelSeries
        | DragStart of bool
        | UpdateVPNSeries of (string * int) list
        | UpdateTORSeries of (string * int) list
        | UpdatePRXSeries of (string * int) list
        | UpdateCOOSeries of (string * int) list
        | UpdateMALSeries of (string * int) list
        | FilterUpdated of FilterUpdatedCommandArgs

    let updateSeries (series: ObservableCollection<SeriesData>) (data: (string * int) list) =

        // Remove items not in data
        let namesInData = data |> List.map fst |> Set.ofList
        let itemsToRemove = 
            series
            |> Seq.filter (fun sd -> not (Set.contains sd.Name namesInData))
            |> Seq.toList

        for item in itemsToRemove do
            series.Remove(item) |> ignore 
        
        // Change values with matching names, add new ones
        data |> List.iter (fun (name, count) ->
            match series |> Seq.mapi (fun i sd -> (i, sd)) |> Seq.tryFind (fun (_, sd) -> sd.Name = name) with
            | Some(i, seriesData) ->
                // Replace existing item at the same index
                let updatedItem = { seriesData with Count = count }
                series.[i] <- updatedItem
            | None ->
                // Add new item
                let newItem = { Name = name; Count = count; Geography = "" }
                series.Add(newItem))
        
        series

    let updateFilterFromData (filter: FilterItem List) (data: ObservableCollection<SeriesData>) =
        // If the given data contains items not present in the filter, add them with show = true
        let namesInData = data |> Seq.map (fun sd -> sd.Name) |> Set.ofSeq
        let itemsToAdd = 
            data
            |> Seq.filter (fun sd -> not (Set.contains sd.Name namesInData))
            |> Seq.map (fun sd -> { Name = sd.Name; Show = true })
            |> Seq.toList
        filter
        |> List.append itemsToAdd
        

    let updateSeriesFilter (seriesFilter : FilterItem List) (args : FilterUpdatedCommandArgs) =
        // replace the FilterItem in the given series with a new one
        //printfn $"UpdateSeriesFilter - Series: {args.SeriesName} - Filter: {args.FilterName} - Status: {args.FilterStatus}"
        seriesFilter 
        |> List.map (fun item -> 
            if item.Name = args.FilterName then { item with Show = args.FilterStatus } 
            else item)
        
    let updateFilter (model : Model) (args : FilterUpdatedCommandArgs) =
        // replace the FilterItem in the given series with a new one, if required
        match args.SeriesName with
        | "VPN" -> { model with VPNFilter = updateSeriesFilter model.VPNFilter args }
        | "TOR" -> { model with TORFilter = updateSeriesFilter model.TORFilter args }
        | "PRX" -> { model with PRXFilter = updateSeriesFilter model.PRXFilter args }
        | "COO" -> { model with COOFilter = updateSeriesFilter model.COOFilter args }
        | "MAL" -> { model with MALFilter = updateSeriesFilter model.MALFilter args }
        | _ -> model
            
        
    


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

    let fetchDataForVPNChart (dispatch: Msg -> unit) =
        let timer = new Timer(Random().Next(2990,3010))
        let disposable =
            timer.Elapsed.Subscribe(fun _ ->
                async {
                    let! data = fetchDataAsync("vpn")
                    dispatch (UpdateVPNSeries data)
                } |> Async.Start
            )
        timer.Start()
        disposable
    
    let fetchDataForTORChart (dispatch: Msg -> unit) =
        let timer = new Timer(Random().Next(2990,3010))
        let disposable =
            timer.Elapsed.Subscribe(fun _ ->
                async {
                    let! data = fetchDataAsync("tor")
                    dispatch (UpdateTORSeries data)
                } |> Async.Start
            )
        timer.Start()
        disposable
    
    let fetchDataForPRXChart (dispatch: Msg -> unit) =
        let timer = new Timer(Random().Next(2990,3010))
        let disposable =
            timer.Elapsed.Subscribe(fun _ ->
                async {
                    let! data = fetchDataAsync("proxy")
                    dispatch (UpdatePRXSeries data)
                } |> Async.Start
            )
        timer.Start()
        disposable
    
    let fetchDataForCOOChart (dispatch: Msg -> unit) =
        let timer = new Timer(Random().Next(2990,3010))
        let disposable =
            timer.Elapsed.Subscribe(fun _ ->
                async {
                    let! data = fetchDataAsync("cc")
                    dispatch (UpdateCOOSeries data)
                } |> Async.Start
            )
        timer.Start()
        disposable
    
    let fetchDataForMALChart (dispatch: Msg -> unit) =
        let timer = new Timer(Random().Next(2990,3010))
        let disposable =
            timer.Elapsed.Subscribe(fun _ ->
                async {
                    let! data = fetchDataAsync("malware")
                    dispatch (UpdateMALSeries data)
                } |> Async.Start
            )
        timer.Start()
        disposable

    let mapSourceDataToSeriesData(data: list<string * int>) : ObservableCollection<SeriesData> =
        let seriesData = ObservableCollection<SeriesData>()
        for item in data do
            seriesData.Add({Name = fst item; Count = snd item; Geography = ""})
        seriesData

    let fetchPieDataAsync (dataType: string) =
        async {
            let! data = fetchDataAsync(dataType)
            let series = mapSourceDataToSeriesData data
            return series
        }

    let setPanelSeries (model : Model) =        
        // TODO: Abstract the string matching to generalize the solution
        for panel in model.Panels do
            match panel.SeriesName with
            | "VPN" -> 
                panel.SeriesList <- model.VPNSeries
                panel.FilterList <- model.VPNFilter
            | "TOR" -> 
                panel.SeriesList <- model.TORSeries
                panel.FilterList <- model.TORFilter
            | "PRX" -> 
                panel.SeriesList <- model.PRXSeries
                panel.FilterList <- model.PRXFilter
            | "COO" -> 
                panel.SeriesList <- model.COOSeries
                panel.FilterList <- model.COOFilter
            | "MAL" -> 
                panel.SeriesList <- model.MALSeries
                panel.FilterList <- model.MALFilter
            | _ -> ()

        model
    
    let init() =
        async {
            let! vpnSeries = fetchPieDataAsync("vpn")
            let! torSeries = fetchPieDataAsync("tor")
            let! prxSeries = fetchPieDataAsync("proxy")
            let! cooSeries = fetchPieDataAsync("cc")
            let! malSeries = fetchPieDataAsync("malware")
        
            return {
                IsLoading = false
                IsFrozen = false
                TimeFrame = "todo"
                Panels = [ 
                            DragPanel(SeriesName="VPN", PosX=10.0, PosY=10.0)
                            DragPanel(SeriesName="TOR", PosX=220.0, PosY=10.0)
                            DragPanel(SeriesName="PRX", PosX=430.0, PosY=10.0)
                            DragPanel(SeriesName="MAL", PosX=640.0, PosY=10.0)
                            DragPanel(SeriesName="COO", PosX=10.0, PosY=220.0, Width=600, Height=400, ChartType=EZChartType.GeoMap)
                        ]
                VPNSeries = vpnSeries                
                TORSeries = torSeries
                PRXSeries = prxSeries
                COOSeries = cooSeries
                MALSeries = malSeries
                // set default visibility for all series
                VPNFilter = vpnSeries |> Seq.map (fun sd -> { Name = sd.Name; Show = true }) |> Seq.toList
                TORFilter = torSeries |> Seq.map (fun sd -> { Name = sd.Name; Show = true }) |> Seq.toList
                PRXFilter = prxSeries |> Seq.map (fun sd -> { Name = sd.Name; Show = true }) |> Seq.toList
                COOFilter = cooSeries |> Seq.map (fun sd -> { Name = sd.Name; Show = true }) |> Seq.toList
                MALFilter = malSeries |> Seq.map (fun sd -> { Name = sd.Name; Show = true }) |> Seq.toList
                
                IsDragging = false
            }
        } |> Async.RunSynchronously,
        Cmd.ofEffect (fun dispatch ->
            //printfn "Dashboard init"
            dispatch SetPanelSeries
        )
    
    let update msg model =
        match msg with
        | OpenPanel name ->
            // Create new panel with given series
            model, Cmd.none
        | ClosePanel index ->
            // Remove panel at index
            model, Cmd.none
        | SetPanelSeries ->
            // Assign correct series to panels by name
            setPanelSeries model |> ignore
            for panel in model.Panels do
                for item in panel.SeriesList do
                    printfn $"{item.Name} : {item.Count} : {item.Geography}"
            
            model, Cmd.none
        | DragStart bDragging ->
            { model with IsDragging = bDragging }, Cmd.none

        | UpdateVPNSeries data ->
            // update VPNSeries with new data
            let series = updateSeries model.VPNSeries data
            let filter = updateFilterFromData model.VPNFilter series
            { model with VPNSeries = series; VPNFilter = filter }, Cmd.none
        | UpdateTORSeries data ->
            // update TORSeries with new data
            let series = updateSeries model.TORSeries data
            let filter = updateFilterFromData model.TORFilter series
            { model with TORSeries = series; TORFilter = filter }, Cmd.none
        | UpdatePRXSeries data ->
            // update PRXSeries with new data
            let series = updateSeries model.PRXSeries data
            let filter = updateFilterFromData model.PRXFilter series
            { model with PRXSeries = series; PRXFilter = filter }, Cmd.none
        | UpdateCOOSeries data ->
            // update COOSeries with new data
            let series = updateSeries model.COOSeries data
            let filter = updateFilterFromData model.COOFilter series
            { model with COOSeries = series; COOFilter = filter }, Cmd.none
        | UpdateMALSeries data ->
            // update MALSeries with new data
            let series = updateSeries model.MALSeries data
            let filter = updateFilterFromData model.MALFilter series
            { model with MALSeries = series; MALFilter = filter }, Cmd.none
        | FilterUpdated args ->
            // update given series with new filter item status
            updateFilter model args, Cmd.none
        
        

    let subscriptions (model : Model) : Sub<Msg> =
        [
            [ nameof fetchDataForVPNChart], fetchDataForVPNChart
            [ nameof fetchDataForTORChart], fetchDataForTORChart
            [ nameof fetchDataForPRXChart], fetchDataForPRXChart
            [ nameof fetchDataForCOOChart], fetchDataForCOOChart
            [ nameof fetchDataForMALChart], fetchDataForMALChart            
        ]

open Dashboard
open ReactiveUI

type DashboardViewModel() =
    inherit ReactiveElmishViewModel()

    let local =
        Program.mkAvaloniaProgram init update
        |> Program.withSubscription subscriptions
        |> Program.mkStore

    member this.IsLoading
        with get() = this.Bind(local, _.IsLoading)
    member this.IsFrozen
        with get() = this.Bind(local, _.IsFrozen)
    member this.TimeFrame
        with get() = this.Bind(local, _.TimeFrame)
    
    member this.Panels
        with get() = this.Bind(local, _.Panels)
        
    
    member this.VPNSeries
        with get() = this.Bind(local, _.VPNSeries)
    member this.TORSeries
        with get() = this.Bind(local, _.TORSeries)
    member this.PRXSeries
        with get() = this.Bind(local, _.PRXSeries)
    member this.COOSeries
        with get() = this.Bind(local, _.COOSeries)    
    member this.MALSeries
        with get() = this.Bind(local, _.MALSeries)
    

    member this.IsDragging
        with get() = this.Bind(local, _.IsDragging)
        and set(value) = local.Dispatch (DragStart value)
    
    member val FilterUpdatedCommand = ReactiveCommand.Create<FilterUpdatedCommandArgs, unit>(fun args ->  
        local.Dispatch (FilterUpdated args)
        ) with get, set

    static member DesignVM =
        new DashboardViewModel()