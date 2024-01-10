namespace AidenDesktop.Models

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
    let mutable _seriesName : string = "VPN"
    // TODO: SeriesList would be better as an option type, but that seems to break
    // the binding.  Possibly the elmish model not updating properly?
    let mutable _seriesList = ObservableCollection<SeriesData>(
        [
            { Name = "FoxyProxy"; Count = 10; Geography = "" }
            { Name = "Nord"; Count = 10; Geography = "" }
        ])
    let mutable _posX : double = 0.0
    let mutable _posY : double = 0.0
    let mutable _width : double = 200.0
    let mutable _height : double = 200.0

    member this.SeriesName
        with get() = _seriesName
        and set(value) = _seriesName <- value
    
    member this.SeriesList
        with get() = _seriesList
        and set(value) = _seriesList <- value
    
    member this.PosX
        with get() = _posX
        and set(value) = _posX <- value
    
    member this.PosY
        with get() = _posY
        and set(value) = _posY <- value
    
    member this.Width
        with get() = _width
        and set(value) = _width <- value
    
    member this.Height
        with get() = _height
        and set(value) = _height <- value