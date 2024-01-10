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
    let SeriesListProperty = AvaloniaProperty.Register<EZGeo, ObservableCollection<SeriesData>>("SeriesList", ObservableCollection<SeriesData>())

    let blueSeries = [|
        SKColor.Parse("#5e56f5").AsLvcColor(); // LightBlue
        SKColor.Parse("#2d2899").AsLvcColor(); // Blue
        SKColor.Parse("#100c52").AsLvcColor()  // DeepBlue
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

    member this.ActualSeries
        with get() = _seriesValues

    member this.SyncObservable (sender : obj) (e : NotifyCollectionChangedEventArgs) =
        let targetCollection = _seriesValues.[0].Lands
        match e.Action with
        | NotifyCollectionChangedAction.Add ->
            ()
        | NotifyCollectionChangedAction.Remove ->
            ()
        | NotifyCollectionChangedAction.Replace ->
            ()
        | NotifyCollectionChangedAction.Move ->
            ()
        | NotifyCollectionChangedAction.Reset ->
            ()
        | _ -> ()

    override this.OnApplyTemplate(e) =
        base.OnApplyTemplate(e)
        
