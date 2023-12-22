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

        }
    
    type Msg =
        | OpenPanel of String
        | ClosePanel of int
        | SetPanelSeries
        | DragStart of bool

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
        printfn "setPanelSeries Called..."
        for panel in model.Panels do
            match panel.SeriesName with
            | "VPN" -> 
                panel.SeriesList <- model.VPNSeries
                printfn "VPN Series Set:"
                for item in panel.SeriesList do
                    printfn $"{item.Name} : {item.Count} : {item.Geography}"
            
                
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
                            DragPanel(SeriesName="COO", PosX=10.0, PosY=220.0)
                        ]
                VPNSeries = vpnSeries                
                TORSeries = torSeries
                PRXSeries = prxSeries
                COOSeries = cooSeries
                MALSeries = malSeries
                
                IsDragging = false
            }
        } |> Async.RunSynchronously,
        Cmd.ofEffect (fun dispatch ->
            printfn "Dashboard init"
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



open Dashboard

type DashboardViewModel() =
    inherit ReactiveElmishViewModel()

    let local =
        Program.mkAvaloniaProgram init update
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

    static member DesignVM =
        new DashboardViewModel()