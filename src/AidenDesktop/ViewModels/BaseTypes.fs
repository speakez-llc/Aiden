namespace AidenDesktop.ViewModels

open System
open System.Collections.ObjectModel


/// <summary>
/// For use in ObservableCollection<SeriesData> lists
/// </summary>
type SeriesData =
    {
        Name: string
        Count: int
        Geography: string
    }

type DragPanel() =
    let mutable _seriesName : string = ""
    let mutable _seriesList = None : ObservableCollection<SeriesData> option


    member this.SeriesName
        with get() = _seriesName
        and set(value) = _seriesName <- value
    
    member this.SeriesList
        with get() = _seriesList
        and set(value) = _seriesList <- value