namespace AidenDesktop.Views

open System
open AidenDesktop.ViewModels
open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml
open LiveChartsCore.SkiaSharpView.Avalonia

type GeoMapView () as this =
    inherit UserControl ()

    do
        this.DataContextChanged
            .Subscribe(fun _ ->
                match this.DataContext with
                | :? GeoMapViewModel as viewModel ->
                    printfn "Initializing DoughnutView"
                    this.InitializeComponent()
                    let geoMap = this.FindControl<GeoMap>("GeoMap")
                    let handleGeoMapDetachedFromVisualTree (dispatch: GeoMap.Msg -> unit) (sender: Object) (args: VisualTreeAttachmentEventArgs) =
                        if sender <> null && sender :? GeoMap then
                            dispatch (GeoMap.SetFetchDataForCOOChartActive false)
                    let handleGeoMapAttachedToVisualTree (dispatch: GeoMap.Msg -> unit) (sender: Object) (args: VisualTreeAttachmentEventArgs) =
                        if sender <> null && sender :? GeoMap then
                            // re-initialize the geo map
                            
                            dispatch (GeoMap.SetFetchDataForCOOChartActive true)
                    let attachEventHandlers (dispatch: GeoMap.Msg -> unit) (geoMap: GeoMap) =
                        printfn "Attaching event handlers to GeoMap"
                        geoMap.add_DetachedFromVisualTree(EventHandler<VisualTreeAttachmentEventArgs>(handleGeoMapDetachedFromVisualTree dispatch))
                        geoMap.add_AttachedToVisualTree(EventHandler<VisualTreeAttachmentEventArgs>(handleGeoMapAttachedToVisualTree dispatch))
                    attachEventHandlers viewModel.Dispatch geoMap
                | _ -> ()
            ) |> ignore

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)