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


    static let SeriesTypesProperty = AvaloniaProperty.Register<SeriesBox, string list>("SeriesTypes", ["VPN"; "TOR"; "PRX"; "COO"])

    static let ChartTypesProperty = AvaloniaProperty.Register<SeriesBox, string list>("ChartTypes", ["Pie"; "Bar"; "Line"; "Area"])
   
    static let IsResizableProperty = AvaloniaProperty.Register<SeriesBox, bool>("IsResizable", true)
    
    static let IsMovableProperty = AvaloniaProperty.Register<SeriesBox, bool>("IsMovable", true)
   
    static let PosXProperty = AvaloniaProperty.Register<SeriesBox, double>("PosX", 0.0)
    static let PosYProperty = AvaloniaProperty.Register<SeriesBox, double>("PosY", 0.0)

    static let SelectedChartTypeProperty = AvaloniaProperty.Register<SeriesBox, EZChartType>("SelectedChartType", EZChartType.Pie)

    

    let mutable _isDragging : bool = false
    let mutable _isResizing : bool = false
    let mutable _dragPos : Point = Point(0.0, 0.0)

    let _resizeHandleSize = 24.0

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
    
    member this.SeriesTypes
        with get() = this.GetValue(SeriesTypesProperty)
        and set(value) =
            this.SetValue(SeriesTypesProperty, value) |> ignore
    
    member this.ChartTypes
        with get() = this.GetValue(ChartTypesProperty)
        and set(value) =
            this.SetValue(ChartTypesProperty, value) |> ignore
    member this.SelectedChartType
        with get() = this.GetValue(SelectedChartTypeProperty)
        and set(value) =
            this.SetValue(SelectedChartTypeProperty, value) |> ignore

    member this.IsResizable
        with get() = this.GetValue(IsResizableProperty)
        and set(value) =
            this.SetValue(IsResizableProperty, value) |> ignore
    
    member this.IsMovable
        with get() = this.GetValue(IsMovableProperty)
        and set(value) =
            this.SetValue(IsMovableProperty, value) |> ignore

    member this.PosX
        with get() = this.GetValue(PosXProperty)
        and set(value) =
            this.SetValue(PosXProperty, value) |> ignore
            this.NotifyPropertyChanged("PosX")
    
    member this.PosY
        with get() = this.GetValue(PosYProperty)
        and set(value) =
            this.SetValue(PosYProperty, value) |> ignore
            this.NotifyPropertyChanged("PosY")

    member this.ShowPie
        with get() = this.SelectedChartType = EZChartType.Pie
    
    member this.ShowMap
        with get() = this.SelectedChartType = EZChartType.GeoMap


    member this.BackEndX
        with get() = this.Width / 2.0
    
    member this.BackEndY
        with get() = this.Height / 2.0

    member private this.NotifyPropertyChanged(propertyName : string) =
        printfn $"SeriesBox NotifyPropertyChanged: {propertyName}"
        propertyChanged.Trigger(this, PropertyChangedEventArgs(propertyName))

    member private this._IsResizeZone(loc : Point) =
        // Resize zone is the bottom right corner
        loc.X > this.Width - _resizeHandleSize && loc.Y > this.Height - _resizeHandleSize
        
        
        
    member private this._IsMoveZone(loc : Point) =
        loc.Y < _resizeHandleSize

    override this.OnApplyTemplate(e) =
        base.OnApplyTemplate(e)
        printfn "SeriesBox OnApplyTemplate"
        

    override this.OnPropertyChanged(e : AvaloniaPropertyChangedEventArgs) =
        base.OnPropertyChanged(e)
        //printfn $"SeriesBox OnPropertyChanged: {e.Property.Name}"

    override this.OnPointerPressed(e : PointerPressedEventArgs) =
        base.OnPointerPressed(e)
        printfn $"SeriesBox OnPointerPressed {e.GetCurrentPoint(this).Position}"
        let pos = e.GetCurrentPoint(this).Position
        if this._IsResizeZone(pos) then
            _isDragging <- true
            _isResizing <- true
            _dragPos <- pos
        else if this._IsMoveZone(pos) then
            _isDragging <- true
            _isResizing <- false
            _dragPos <- pos
        else
            _isDragging <- false
            _isResizing <- false
        ()

    
    override this.OnPointerReleased(e : PointerReleasedEventArgs) =
        base.OnPointerReleased(e)
        printfn "SeriesBox OnPointerReleased"
        _isDragging <- false
        _isResizing <- false
        ()
    
    override this.OnPointerMoved(e : PointerEventArgs) =
        base.OnPointerMoved(e)
        let pos = e.GetCurrentPoint(this).Position

        if this._IsResizeZone(pos) then
            this.Cursor <- new Cursor(StandardCursorType.SizeAll)
        else if this._IsMoveZone(pos) then
            this.Cursor <- new Cursor(StandardCursorType.Hand)
        else
            this.Cursor <- new Cursor(StandardCursorType.Arrow)
        
        if _isDragging && _isResizing then
            let deltaX = pos.X - _dragPos.X
            let deltaY = pos.Y - _dragPos.Y
            this.Width <- this.Width + deltaX
            this.Height <- this.Height + deltaY
            _dragPos <- pos
            this.NotifyPropertyChanged("Width")
            this.NotifyPropertyChanged("Height")
            this.InvalidateVisual()
            
        else if _isDragging && not _isResizing then
            let deltaX = pos.X - _dragPos.X
            let deltaY = pos.Y - _dragPos.Y
            this.PosX <- this.PosX + deltaX
            this.PosY <- this.PosY + deltaY
            _dragPos <- pos
            this.NotifyPropertyChanged("PosX")
            this.NotifyPropertyChanged("PosY")
            this.InvalidateVisual()
            
        ()

    
        