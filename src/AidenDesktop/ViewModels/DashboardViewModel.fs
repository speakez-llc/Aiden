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

            IsDragging : bool
            ColorMap : string[]
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
        for panel in model.Panels do
            match panel.SeriesName with
            | "VPN" -> panel.SeriesList <- model.VPNSeries
            | "TOR" -> panel.SeriesList <- model.TORSeries
            | "PRX" -> panel.SeriesList <- model.PRXSeries
            | "COO" -> panel.SeriesList <- model.COOSeries
            | "MAL" -> panel.SeriesList <- model.MALSeries
            | _ -> ()

        model //{ model with Panels = model.Panels }
    
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
                            //DragPanel(SeriesName="COO", PosX=850.0, PosY=10.0)
                            DragPanel(SeriesName="COO", PosX=10.0, PosY=220.0, Width=830.0, Height=550.0, ChartType=EZChartType.GeoMap)
                        ]
                VPNSeries = vpnSeries                
                TORSeries = torSeries
                PRXSeries = prxSeries
                COOSeries = cooSeries
                MALSeries = malSeries
                
                IsDragging = false
                //ColorMap = [|"#5e56f5"; "#2d2899"; "#100c52"|]
                ColorMap = [|"#47cc47"; "#0cab0c"; "#036603"; "#5e56f5"; "#2d2899"; "#100c52" |]
            }
        } |> Async.RunSynchronously,
        Cmd.ofEffect (fun dispatch ->
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
            { model with VPNSeries = series }, Cmd.none
        | UpdateTORSeries data ->
            // update TORSeries with new data
            let series = updateSeries model.TORSeries data
            { model with TORSeries = series}, Cmd.none
        | UpdatePRXSeries data ->
            // update PRXSeries with new data
            let series = updateSeries model.PRXSeries data
            { model with PRXSeries = series}, Cmd.none
        | UpdateCOOSeries data ->
            // update COOSeries with new data
            let series = updateSeries model.COOSeries data
            { model with COOSeries = series}, Cmd.none
        | UpdateMALSeries data ->
            // update MALSeries with new data
            let series = updateSeries model.MALSeries data
            { model with MALSeries = series}, Cmd.none

    let subscriptions (model : Model) : Sub<Msg> =
        [
            [ nameof fetchDataForVPNChart], fetchDataForVPNChart
            [ nameof fetchDataForTORChart], fetchDataForTORChart
            [ nameof fetchDataForPRXChart], fetchDataForPRXChart
            [ nameof fetchDataForCOOChart], fetchDataForCOOChart
            [ nameof fetchDataForMALChart], fetchDataForMALChart            
        ]

open Dashboard

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
    

    member this.IsDragging
        with get() = this.Bind(local, _.IsDragging)
        and set(value) = local.Dispatch (DragStart value)
    
    member this.ColorMap
        with get() = this.Bind(local, _.ColorMap)

    static member DesignVM =
        new DashboardViewModel()