namespace AidenDesktop.ViewModels

open System
open System.Collections.ObjectModel
open ReactiveElmish
open ReactiveElmish.Avalonia
open Elmish
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
        }
    
    type Msg =
        | OpenPanel of String
        | ClosePanel of int
    
    let init() =
        {
            IsLoading = false
            IsFrozen = false
            TimeFrame = "todo"
            Panels = [ DragPanel()]
            VPNSeries = ObservableCollection<SeriesData>
                [
                    {Name = "Foxyproxy"; Count = 10; Geography = ""}
                    {Name = "Nord"; Count = 5; Geography = ""}
                    {Name = "Express"; Count = 3; Geography = ""}
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
                    {Name = "Foxyproxy"; Count = 10; Geography = "USA"}
                    {Name = "Nord"; Count = 5; Geography = "RUS"}
                    {Name = "Express"; Count = 3; Geography = "CAN"}
                ]
        },
        Cmd.ofEffect (fun dispatch ->
            printfn "Dashboard init"

        )
    
    let update msg model =
        match msg with
        | OpenPanel name ->
            // Create new panel with given series
            model, Cmd.none
        | ClosePanel index ->
            // Remove panel at index
            model, Cmd.none


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
    

    static member DesignVM =
        new DashboardViewModel()