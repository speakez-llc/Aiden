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


type EZGeo() =
    inherit TemplatedControl()

    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()



    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChanged.Publish

    member this.add_PropertyChanged(handler) = propertyChanged.Publish.AddHandler(handler)
    member this.remove_PropertyChanged(handler) = propertyChanged.Publish.RemoveHandler(handler)

    member private this.NotifyPropertyChanged(propertyName : string) =
        printfn $"EZGeo NotifyPropertyChanged: {propertyName}"
        propertyChanged.Trigger(this, PropertyChangedEventArgs(propertyName))
