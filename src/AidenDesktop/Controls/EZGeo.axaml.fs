namespace AidenDesktop.Controls

open System.ComponentModel
open System.Collections.ObjectModel
open System.Collections.Specialized
open Avalonia
open Avalonia.Threading
open Avalonia.Controls.Primitives
open LiveChartsCore
open LiveChartsCore.SkiaSharpView
open LiveChartsCore.SkiaSharpView.Drawing.Geometries
open LiveChartsCore.Geo
open LiveChartsCore.Defaults
open SkiaSharp
open AidenDesktop.Models


type EZGeo() =
    inherit TemplatedControl()

    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()

    // SeriesData source list
    static let SeriesListProperty = AvaloniaProperty.Register<EZGeo, ObservableCollection<SeriesData>>("SeriesList", ObservableCollection<SeriesData>())

    static let ColorMapProperty = AvaloniaProperty.Register<EZGeo, string[]>("ColorMap", [|"#5e56f5"; "#2d2899"; "#100c52"|])

    let blueSeries = [|
        SKColor.Parse("#8e89f8").AsLvcColor(); // LightBlue
        SKColor.Parse("#5e56f5").AsLvcColor(); // LightBlue
        SKColor.Parse("#2d2899").AsLvcColor(); // Blue
        SKColor.Parse("#100c52").AsLvcColor()  // DeepBlue
        SKColor.Parse("#0b0837").AsLvcColor()  // DeepBlue
    |]

    let mutable _seriesValues : HeatLandSeries[] = [| HeatLandSeries(HeatMap = blueSeries, Lands = [|
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

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChanged.Publish

    member this.add_PropertyChanged(handler) = propertyChanged.Publish.AddHandler(handler)
    member this.remove_PropertyChanged(handler) = propertyChanged.Publish.RemoveHandler(handler)

    member private this.NotifyPropertyChanged(propertyName : string) =
        printfn $"EZGeo NotifyPropertyChanged: {propertyName}"
        propertyChanged.Trigger(this, PropertyChangedEventArgs(propertyName))

    member this.SeriesList
        with get() = this.GetValue(SeriesListProperty)
        and set(value) = 
            this.SetValue(SeriesListProperty, value) |> ignore

    member this.ColorMap
        with get() = this.GetValue(ColorMapProperty)
        and set(value) = 
            this.SetValue(ColorMapProperty, value) |> ignore

    member this.ActualSeries
        with get() = _seriesValues

    member this.SyncObservable (sender : obj) (e : NotifyCollectionChangedEventArgs) =
        let targetCollection = _seriesValues.[0].Lands |> ResizeArray
        match e.Action with
        | NotifyCollectionChangedAction.Add ->
            printfn $"EZGeo Sync:Add"
            e.NewItems |> Seq.cast<SeriesData> |> Seq.iter (fun item ->
                printfn $"{item.Name} : {item.Count}"
                let heatland = HeatLand(Name = item.Name, Value = item.Count) :> IWeigthedMapLand
                targetCollection.Add(heatland)
            )
            _seriesValues.[0].Lands <- targetCollection.ToArray()
            ()
        | NotifyCollectionChangedAction.Remove ->
            printfn "EZGeo Sync:Remove"
            let oldItems = e.OldItems |> Seq.cast<SeriesData>
            for item in oldItems do
                printfn $"{item.Name} : {item.Count}"
                let index = targetCollection |> Seq.tryFindIndex (fun s -> s.Name = item.Name)
                match index with
                | Some i -> targetCollection.RemoveAt(i)
                | _ -> ()
            _seriesValues.[0].Lands <- targetCollection.ToArray()
            ()
        | NotifyCollectionChangedAction.Replace ->
            printfn "EZGeo Sync:Replace"
            let newItems = e.NewItems |> Seq.cast<SeriesData>
            for item in newItems do
                printfn $"{item.Name} : {item.Count}"
                let index = targetCollection |> Seq.tryFindIndex (fun s -> s.Name = item.Name)
                match index with
                | Some i -> targetCollection.[i] <- HeatLand(Name = item.Name, Value = item.Count) :> IWeigthedMapLand
                | _ -> ()
            _seriesValues.[0].Lands <- targetCollection.ToArray()
            ()
        | NotifyCollectionChangedAction.Move ->
            ()
        | NotifyCollectionChangedAction.Reset ->
            targetCollection.Clear()
            _seriesValues.[0].Lands <- targetCollection.ToArray()
            ()
        | _ -> ()

        Dispatcher.UIThread.InvokeAsync(fun () -> this.InvalidateVisual() |> ignore) |> ignore

    override this.OnApplyTemplate(e) =
        base.OnApplyTemplate(e)

        this.SeriesList.CollectionChanged.AddHandler(this.SyncObservable)
    
    override this.OnPropertyChanged(e : AvaloniaPropertyChangedEventArgs) =
        base.OnPropertyChanged(e)

        match e.Property.Name with
        | "SeriesList" ->
            let targetCollection = _seriesValues.[0].Lands |> ResizeArray
            targetCollection.Clear()
            let series = this.SeriesList
            for item in series do
                let heatland = HeatLand(Name = item.Name, Value = item.Count) :> IWeigthedMapLand
                targetCollection.Add(heatland)
            _seriesValues.[0].Lands <- targetCollection.ToArray()
            this.NotifyPropertyChanged("SeriesList")
        | "ColorMap" ->
            let colors = this.ColorMap |> Array.map (fun c -> SKColor.Parse(c).AsLvcColor())
            _seriesValues.[0].HeatMap <- colors
            this.NotifyPropertyChanged("ColorMap")
        | _ -> ()
