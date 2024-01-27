namespace AidenDesktop.Controls

open System
open System.ComponentModel
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Primitives

type DragPanel() =
    inherit TemplatedControl()

    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()

    static let DragZoneProperty = AvaloniaProperty.Register<DragPanel, Canvas>("DragZone", Canvas())
    static let ContentProperty = AvaloniaProperty.Register<DragPanel, Object>("Content", Object())
    static let DragPositionProperty = AvaloniaProperty.Register<DragPanel, Point>("DragPosition", Point(0.0, 0.0))

    let mutable _isDragging = false
    let mutable _dragOffset = Point(0.0, 0.0)

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChanged.Publish
    member this.add_PropertyChanged(handler) = propertyChanged.Publish.AddHandler(handler)
    member this.remove_PropertyChanged(handler) = propertyChanged.Publish.RemoveHandler(handler)

    member this.DragZone
        with get() = this.GetValue(DragZoneProperty)
        and set(value) = this.SetValue(DragZoneProperty, value) |> ignore

    member this.Content
        with get() = this.GetValue(ContentProperty)
        and set(value) = this.SetValue(ContentProperty, value) |> ignore
    
    member this.DragPosition
        with get() = this.GetValue(DragPositionProperty)
        and set(value) = 
            this.SetValue(DragPositionProperty, value) |> ignore
            this.NotifyPropertyChanged("DragPosition")


    override this.OnApplyTemplate(e) =
        base.OnApplyTemplate(e)

        //let dragPanel = this.GetTemplateChild("DragPanel") :?> 

    member private this.NotifyPropertyChanged(propertyName : string) =
        //printfn $"DragPanel NotifyPropertyChanged: {propertyName}"
        propertyChanged.Trigger(this, PropertyChangedEventArgs(propertyName))

    override this.OnPointerPressed(e) =
        base.OnPointerPressed(e)
        _isDragging <- true
        //printfn $"OnPointerPressed"
        let pos = e.GetCurrentPoint(this).Position
        _dragOffset <- pos
        //printfn $"pos: {pos}"
        //this.SetValue(Canvas.LeftProperty, pos.X)
        //this.SetValue(Canvas.TopProperty, pos.Y)
        //this.CapturePointer(e)
        e.Handled <- true
    
    override this.OnPointerMoved(e) =
        base.OnPointerMoved(e)
        //printfn $"OnPointerMoved"
        if _isDragging then
            let pos = e.GetCurrentPoint(this.DragZone).Position
            let posx = pos.X - _dragOffset.X
            let posy = pos.Y - _dragOffset.Y
            this.SetValue(Canvas.LeftProperty, posx) |> ignore
            this.SetValue(Canvas.TopProperty, posy) |> ignore
        //this.CapturePointer(e)
        e.Handled <- true

    override this.OnPointerReleased(e) =
        base.OnPointerReleased(e)
        _isDragging <- false
        // $"OnPointerReleased"
        let pos = e.GetCurrentPoint(this).Position
        //printfn $"pos: {pos}"
        //this.SetValue(Canvas.LeftProperty, pos.X)
        //this.SetValue(Canvas.TopProperty, pos.Y)
        //this.CapturePointer(e)
        e.Handled <- true