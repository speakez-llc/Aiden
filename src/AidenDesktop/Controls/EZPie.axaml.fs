namespace AidenDesktop.Controls

open System.ComponentModel
open System.Collections.ObjectModel
open System.Collections.Specialized
open Avalonia
open Avalonia.Threading
open Avalonia.Controls.Primitives
open LiveChartsCore 
open LiveChartsCore.SkiaSharpView
open LiveChartsCore.Defaults
open AidenDesktop.Models

(* *********************************************************
    EZPie - Pie chart wrapper for use in SeriesBox control

*)

type EZPie() = 
    inherit TemplatedControl()

    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()

    static let ValueProperty = AvaloniaProperty.Register<EZPie, double>("Value", 0.0)
    static let SeriesValuesProperty = AvaloniaProperty.Register<EZPie, ObservableCollection<ISeries>>("SeriesValues", ObservableCollection<ISeries>())

    static let SeriesListProperty = AvaloniaProperty.Register<EZPie, ObservableCollection<SeriesData>>("SeriesList", ObservableCollection<SeriesData>())
    
    static let ChartWidthProperty = AvaloniaProperty.Register<EZPie, double>("ChartWidth", 200.0)
    static let ChartHeightProperty = AvaloniaProperty.Register<EZPie, double>("ChartHeight", 200.0)

    static let MaxRadialWidthProperty = AvaloniaProperty.Register<EZPie, double>("MaxRadialWidth", 50.0)


    let mutable _seriesValues = ResizeArray<ISeries>() 

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChanged.Publish

    member this.add_PropertyChanged(handler) = propertyChanged.Publish.AddHandler(handler)
    member this.remove_PropertyChanged(handler) = propertyChanged.Publish.RemoveHandler(handler)
    

    member private this.NotifyPropertyChanged(propertyName : string) =
        //printfn $"EZPie NotifyPropertyChanged: {propertyName}"
        propertyChanged.Trigger(this, PropertyChangedEventArgs(propertyName))

    member this.Value
        with get() = this.GetValue(ValueProperty)
        and set(value) =
            this.SetValue(ValueProperty, value) |> ignore
            //printfn $"Setter: EZPie Value set {value}"
            this.NotifyPropertyChanged("Value")
            
    
    member this.SeriesValues
        with get() = this.GetValue(SeriesValuesProperty)
        and set(value) =
            this.SetValue(SeriesValuesProperty, value) |> ignore
            

    member this.SeriesList
        with get() = this.GetValue(SeriesListProperty)
        and set(value) =
            this.SetValue(SeriesListProperty, value) |> ignore
            //printfn "Setting Series List..."
            
    member this.ChartWidth
        with get() = this.GetValue(ChartWidthProperty)
        and set(value) =
            this.SetValue(ChartWidthProperty, value) |> ignore
            this.NotifyPropertyChanged("ChartWidth")

    member this.ChartHeight
        with get() = this.GetValue(ChartHeightProperty)
        and set(value) =
            this.SetValue(ChartHeightProperty, value) |> ignore
            this.NotifyPropertyChanged("ChartHeight") 
    
    member this.MaxRadialWidth
        with get() = this.GetValue(MaxRadialWidthProperty)
        and set(value) =
            this.SetValue(MaxRadialWidthProperty, value) |> ignore
            this.NotifyPropertyChanged("MaxRadialWidth")
            
    member this.ActualSeries
        with get() = _seriesValues 

    member this.SyncObservables(sender : obj) (e : NotifyCollectionChangedEventArgs) =
        // update contents of _seriesValues with contents of SeriesList
        let targetCollection = _seriesValues
        match e.Action with
        | NotifyCollectionChangedAction.Add ->
            e.NewItems |> Seq.cast<SeriesData> |> Seq.iter (fun item ->
                let series = PieSeries<int>() 
                series.MaxRadialColumnWidth <- this.MaxRadialWidth
                series.Values <- [item.Count]      
                series.Name <- item.Name      
                targetCollection.Add(series :> ISeries))

        | NotifyCollectionChangedAction.Remove ->
            let oldItems = e.OldItems
            for item in oldItems do
                let oldItem = item :?> SeriesData
                let index = targetCollection |> Seq.tryFindIndex(fun s -> s.Name = oldItem.Name)
                match index with
                | Some idx ->
                    targetCollection.RemoveAt(idx)
                | _ -> ()

            //e.OldItems |> Seq.cast<ISeries> |> Seq.iter (fun item -> targetCollection.Remove(item) |> ignore)
            

        | NotifyCollectionChangedAction.Replace ->
            // Handle replace logic
            let newItems = e.NewItems;   
            for item in newItems do
                let newItem = item :?> SeriesData
                let index = targetCollection |> Seq.tryFindIndex(fun s -> s.Name = newItem.Name)
                match index with
                | Some idx ->
                    targetCollection.[idx].Values <- [newItem.Count]
                | _ ->
                    // TODO:  If it isn't there, add it??
                    printfn $"EZPie:SyncObservables Replace -> index not found for {newItem.Name}"
                    ()
            ()
        | NotifyCollectionChangedAction.Move ->
            // Handle move logic
            printfn $"Move... {e.NewItems} {e.OldItems}"
            // ... change indices??
            ()
        | NotifyCollectionChangedAction.Reset ->
            targetCollection.Clear()
        | _ -> ()

        // TODO: This performs much better that just calling InvalidateVisual, but it still misses some updates...
        Dispatcher.UIThread.InvokeAsync(fun () -> this.InvalidateVisual() |> ignore) |> ignore
        
        
    override this.OnApplyTemplate(e) =
        base.OnApplyTemplate(e)
        
        // Subscribe to series changes to update internal ISeries
        this.SeriesList.CollectionChanged.AddHandler(this.SyncObservables)

        
    
    override this.OnPropertyChanged(e : AvaloniaPropertyChangedEventArgs) =
        base.OnPropertyChanged(e)
        //this.NotifyPropertyChanged(e.Property.Name)
        //printfn $"EZ Property Changed: {e.Property.Name}"
        match e.Property.Name with
        | "Value" ->
            this.NotifyPropertyChanged("Value")
        | "SeriesList" ->
            // This gets called once at initialization, but not when items in the collection change
            this.NotifyPropertyChanged("SeriesList")
            let value = this.GetValue(SeriesListProperty)
            // Initialize ISeries list
            _seriesValues.Clear()
            let series = this.SeriesList
            for item in series do
                let series = PieSeries<int>() 
                series.MaxRadialColumnWidth <- this.MaxRadialWidth
                series.Values <- [item.Count]      
                series.Name <- item.Name       
                _seriesValues.Add(series :> ISeries)
            
        | _ -> ()