namespace AidenDesktop.ViewModels

open System
open System.Collections.ObjectModel
open ReactiveElmish
open ReactiveElmish.Avalonia
open Elmish
open AidenDesktop.Models
open App



module Dashboard =

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

            IsDragging : bool

        }
    
    type Msg =
        | OpenPanel of String
        | ClosePanel of int
        | SetPanelSeries
        | DragStart of bool

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
            | _ -> ()

        model //{ model with Panels = model.Panels }
    
    let init() =
        {
            IsLoading = false
            IsFrozen = false
            TimeFrame = "todo"
            Panels = [ 
                        DragPanel(SeriesName="VPN", PosX=10.0, PosY=10.0)
                        DragPanel(SeriesName="TOR", PosX=220.0, PosY=10.0)
                        DragPanel(SeriesName="PRX", PosX=430.0, PosY=10.0)
                     ]
            VPNSeries = ObservableCollection<SeriesData>
                [
                    {Name = "Voxyproxy"; Count = 10; Geography = ""}
                    {Name = "Vord"; Count = 5; Geography = ""}
                    {Name = "Vexpress"; Count = 3; Geography = ""}
                ]
            TORSeries = ObservableCollection<SeriesData>
                [
                    {Name = "Foxytroxy"; Count = 10; Geography = ""}
                    {Name = "Tjord"; Count = 5; Geography = ""}
                    {Name = "Texpress"; Count = 3; Geography = ""}
                ]
            PRXSeries = ObservableCollection<SeriesData>
                [
                    {Name = "Foxyproxy"; Count = 10; Geography = ""}
                    {Name = "Nord"; Count = 5; Geography = ""}
                    {Name = "Express"; Count = 3; Geography = ""}
                ]
            COOSeries = ObservableCollection<SeriesData>
                [
                    {Name = "USA"; Count = 10; Geography = "World"}
                    {Name = "RUS"; Count = 5; Geography = "World"}
                    {Name = "CAN"; Count = 3; Geography = "World"}
                ]
            
            IsDragging = false
        },
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