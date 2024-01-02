namespace AidenDesktop.Views

open System
open Avalonia.Controls
open Avalonia.Markup.Xaml
open System.ComponentModel
open Avalonia
open Avalonia.VisualTree
open AidenDesktop.ViewModels
open AidenDesktop.Controls
open AidenDesktop.Models
open Avalonia.Threading

type DashboardView() as this =
    inherit UserControl()

(*     let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()
 *)
    let rec subscribeToSeriesBoxes (parent : Visual) (handler : PropertyChangedEventHandler) =
        match parent with
        | :? SeriesBox as seriesBox ->
            printfn $"ADDING HANDLER TO CHILD: {seriesBox.SeriesName}"
            seriesBox.add_PropertyChanged(handler)
        | _ -> ()
        
        for child in parent.GetVisualChildren() do            
            subscribeToSeriesBoxes child handler

        ()
    
    let seriesBoxPropertyChanged (sender : obj) (e : PropertyChangedEventArgs) =
        let box = sender :?> SeriesBox
        let con = box.DataContext :?> DragPanel

        let vm = this.DataContext :?> DashboardViewModel
        let container = this.FindControl<Grid>("DragZone")
        
        let d = Dispatcher.UIThread.InvokeAsync(fun () -> vm.OnPanelChanged(con))
        Dispatcher.UIThread.InvokeAsync(fun () -> container.InvalidateVisual()) |> ignore
        //Dispatcher.UIThread.InvokeAsync(fun () -> container.InvalidateMeasure()) |> ignore
        //Dispatcher.UIThread.InvokeAsync(fun () -> container.InvalidateArrange()) |> ignore
        //Dispatcher.UIThread.InvokeAsync(fun () -> this.InvalidateMeasure()) |> ignore
        //Dispatcher.UIThread.InvokeAsync(fun () -> this.InvalidateArrange()) |> ignore
        //Dispatcher.UIThread.InvokeAsync(fun () -> this.InvalidateVisual() |> ignore) |> ignore
        //printfn $"DashboardView seriesBoxPropertyChanged: {e.PropertyName} - {box.SeriesName}"
        //printfn $"PosX: {con.PosX} - PosY: {con.PosY}"
        ()

    do
        this.InitializeComponent()
    

(*     interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChanged.Publish
        
    member this.add_PropertyChanged(handler) = propertyChanged.Publish.AddHandler(handler)
    member this.remove_PropertyChanged(handler) = propertyChanged.Publish.RemoveHandler(handler)
 *)

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this) |> ignore

        
        (* let vm = this.DataContext :?> DashboardViewModel
        let panels = vm.Panels
        for panel in panels do
            printfn $"Panel: {panel.SeriesName}" *)


        ()

    // TODO: Find Van Helsing... OnApplyTemplate, OnAttachedToVisualTree, and OnInitialized are all
    // called before the contained SeriesBoxes are created.
    override this.OnApplyTemplate(e) =
        printfn $"DashboardView OnApplyTemplate"
        base.OnApplyTemplate(e)
        //subscribeToSeriesBoxes this seriesBoxPropertyChanged
    
    override this.OnAttachedToVisualTree(e) =
        printfn $"DashboardView OnApplyTemplate"
        base.OnAttachedToVisualTree(e)
        //subscribeToSeriesBoxes this seriesBoxPropertyChanged
    
    override this.OnInitialized() =
        printfn $"DashboardView OnInitialized"
        base.OnInitialized()
        //subscribeToSeriesBoxes this seriesBoxPropertyChanged

    override this.OnPropertyChanged(e : AvaloniaPropertyChangedEventArgs) =
        printfn $"DashboardView OnPropertyChanged: {e.Property.Name}"
        base.OnPropertyChanged(e)
        // TODO: Garbage hack to get around SeriesBox creation time
        match e.Property.Name with
        | "Bounds" ->
            subscribeToSeriesBoxes this seriesBoxPropertyChanged
        | _ -> ()
