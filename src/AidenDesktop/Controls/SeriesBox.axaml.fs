namespace AidenDesktop.Controls

open System
open System.ComponentModel
open System.Collections.ObjectModel
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.Media
open Avalonia.Input
open AidenDesktop.Models


type SeriesBox () =
    inherit TemplatedControl ()

    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()

    static let SeriesNameProperty = AvaloniaProperty.Register<SeriesBox, string>("SeriesName", "")
    static let SeriesListProperty = AvaloniaProperty.Register<SeriesBox, ObservableCollection<SeriesData>>("SeriesList", ObservableCollection<SeriesData>())


    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChanged.Publish
    member this.add_PropertyChanged(handler) = propertyChanged.Publish.AddHandler(handler)
    member this.remove_PropertyChanged(handler) = propertyChanged.Publish.RemoveHandler(handler)

    member this.SeriesName
        with get() = this.GetValue(SeriesNameProperty)
        and set(value) =
            this.SetValue(SeriesNameProperty, value) |> ignore
            this.NotifyPropertyChanged("SeriesName")

    member this.SeriesList
        with get() = this.GetValue(SeriesListProperty)
        and set(value) =
            this.SetValue(SeriesListProperty, value) |> ignore
            for item in this.SeriesList do
                printfn $"{item.Name} : {item.Count} : {item.Geography}"

    member private this.NotifyPropertyChanged(propertyName : string) =
        printfn $"SeriesBox NotifyPropertyChanged: {propertyName}"
        propertyChanged.Trigger(this, PropertyChangedEventArgs(propertyName))

    override this.OnApplyTemplate(e) =
        base.OnApplyTemplate(e)
        printfn "SeriesBox OnApplyTemplate"
        for item in this.SeriesList do
            printfn $"{item.Name} : {item.Count} : {item.Geography}"

    override this.OnPropertyChanged(e : AvaloniaPropertyChangedEventArgs) =
        base.OnPropertyChanged(e)
        printfn $"SeriesBox OnPropertyChanged: {e.Property.Name}"