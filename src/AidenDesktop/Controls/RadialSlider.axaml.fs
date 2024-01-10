namespace AidenDesktop.Controls

(*
    To Use:
        1) Add RadialSlider.axaml.fs as a compile target in the project file
        2) Add RadialSlider.axaml as a style resource to App.axaml
        3) Add the controls namespace to the view => <controls:RadialSlider />
*)


open System
open System.ComponentModel
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.Controls.Shapes
open Avalonia.Media
open Avalonia.Input

type RadialSlider () = 
    inherit TemplatedControl ()

    // Property Changed event for binding - used for all properties
    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()

    // Slider specific properties
    // Slider value range 
    /// <summary>
    /// Two-Way bound double value of the slider
    /// </summary>
    static let ValueProperty = AvaloniaProperty.Register<RadialSlider, double>("Value", 0.0)
    /// <summary>
    /// Minimum double value of the slider
    /// </summary>
    static let MinProperty = AvaloniaProperty.Register<RadialSlider, double>("Min", 0.0)
    /// <summary>
    /// Maximum double value of the slider
    /// </summary>
    static let MaxProperty = AvaloniaProperty.Register<RadialSlider, double>("Max", 500.0)
    /// <summary>
    /// Increment step size double for the slider
    /// </summary>
    static let StepProperty = AvaloniaProperty.Register<RadialSlider, double>("Step", 1.0)    
    
    // Slider appearance
    /// <summary>
    /// For now, the slider must be a circle, so it's container is a square
    /// </summary>
    static let SliderSizeProperty = AvaloniaProperty.Register<RadialSlider, double>("SliderSize", 100.0)
    /// <summary>
    /// Margin around the slider
    /// </summary>
    static let SliderMarginProperty = AvaloniaProperty.Register<RadialSlider, double>("SliderMargin", 10.0)

    static let ArcSizeProperty = AvaloniaProperty.Register<RadialSlider, double>("ArcSize", 360.0)
    static let ArcStartProperty = AvaloniaProperty.Register<RadialSlider, double>("ArcStart", 0.0)

    static let RingColorProperty = AvaloniaProperty.Register<RadialSlider, Color>("RingColor", Color.Parse("#FF0000"))
    static let RingWidthProperty = AvaloniaProperty.Register<RadialSlider, double>("RingWidth", 10.0)
    static let HandleBaseColorProperty = AvaloniaProperty.Register<RadialSlider, Color>("HandleBaseColor", Color.Parse("#666666"))
    static let HandleHighlightColorProperty = AvaloniaProperty.Register<RadialSlider, Color>("HandleHighlightColor", Color.Parse("#eeeeee"))
    static let HandleWidthProperty = AvaloniaProperty.Register<RadialSlider, double>("HandleWidth", 20.0)
    static let HandleHeightProperty = AvaloniaProperty.Register<RadialSlider, double>("HandleHeight", 20.0)

    // Value Display
    static let ValueFontSizeProperty = AvaloniaProperty.Register<RadialSlider, double>("ValueFontSize", 20.0)
    static let ValueFontColorProperty = AvaloniaProperty.Register<RadialSlider, Color>("ValueFontColor", Color.Parse("#000000"))

    // Internal properties... TODO: I don't think these need to be full on properties...
    static let HandlePercentProperty = AvaloniaProperty.Register<RadialSlider, double>("HandlePercent", 0.0)
    static let RingStopProperty = AvaloniaProperty.Register<RadialSlider, double>("RingStop", 0.0)
    static let HandlePositionProperty = AvaloniaProperty.Register<RadialSlider, Point>("HandlePosition", Point(0.0,0.0))
    let mutable sliderCanvas : Canvas = Canvas()
    // Dragging state
    let mutable isDragging : bool = false
    

    // Property Changed event implementation
    [<CLIEvent>]
    member this.PropertyChanged = propertyChanged.Publish
    
    interface INotifyPropertyChanged with
        member this.add_PropertyChanged(handler) = propertyChanged.Publish.AddHandler(handler)
        member this.remove_PropertyChanged(handler) = propertyChanged.Publish.RemoveHandler(handler)

    member private this.NotifyPropertyChanged(propertyName: string) =
        propertyChanged.Trigger(this, PropertyChangedEventArgs(propertyName))
        //(this :> INotifyPropertyChanged).PropertyChanged.Trigger(this, PropertyChangedEventArgs(propertyName))

    
    // Slider Properties
    // Slider Value Range 
    member this.Value
        with get() = this.GetValue(ValueProperty)
        and set(value) =
            this.SetValue(ValueProperty, value) |> ignore
            this.NotifyPropertyChanged(nameof this.Value)
            this.NotifyPropertyChanged(nameof this.HandlePosition) // update handle
            this.NotifyPropertyChanged(nameof this.HandlePercent) // update ring
            this.NotifyPropertyChanged(nameof this.RingStop) // update ring color stop

    member this.Min
        with get() = this.GetValue(MinProperty)
        and set(value) =
            this.SetValue(MinProperty, value) |> ignore
            this.NotifyPropertyChanged(nameof this.Min)
    
    member this.Max
        with get() = this.GetValue(MaxProperty)
        and set(value) =
            this.SetValue(MaxProperty, value) |> ignore
            this.NotifyPropertyChanged(nameof this.Max)

    member this.Step
        with get() = this.GetValue(StepProperty)
        and set(value) =
            this.SetValue(StepProperty, value) |> ignore
            this.NotifyPropertyChanged(nameof this.Step)
    
    // Slider Appearance

    member this.SliderSize
        with get() = this.GetValue(SliderSizeProperty)
        and set(value) = this.SetValue(SliderSizeProperty, value) |> ignore

    member this.SliderMargin
        with get() = this.GetValue(SliderMarginProperty)
        and set(value) = this.SetValue(SliderMarginProperty, value) |> ignore

    member this.ArcSize
        with get() = this.GetValue(ArcSizeProperty)
        and set(value) = this.SetValue(ArcSizeProperty, value) |> ignore
    member this.ArcStart
        with get() = this.GetValue(ArcStartProperty)
        and set(value) = this.SetValue(ArcStartProperty, value) |> ignore

    member this.RingColor
        with get() = this.GetValue(RingColorProperty)
        and set(value) = this.SetValue(RingColorProperty, value) |> ignore
    member this.RingWidth
        with get() = this.GetValue(RingWidthProperty)
        and set(value) = this.SetValue(RingWidthProperty, value) |> ignore
    
    
    // Handle Appearance
    member this.HandleBaseColor
        with get() = this.GetValue(HandleBaseColorProperty)
        and set(value) = this.SetValue(HandleBaseColorProperty, value) |> ignore
    member this.HandleHighlightColor
        with get() = this.GetValue(HandleHighlightColorProperty)
        and set(value) = this.SetValue(HandleHighlightColorProperty, value) |> ignore
    member this.HandleWidth
        with get() = this.GetValue(HandleWidthProperty)
        and set(value) = this.SetValue(HandleWidthProperty, value) |> ignore

    member this.HandleHeight
        with get() = this.GetValue(HandleHeightProperty)
        and set(value) = this.SetValue(HandleHeightProperty, value) |> ignore

    member this.ValueFontSize
        with get() = this.GetValue(ValueFontSizeProperty)
        and set(value) = this.SetValue(ValueFontSizeProperty, value) |> ignore

    // Internal Properties
    member this.HandleSize
        with get() = Point(this.HandleWidth, this.HandleHeight)
    member this.SmallHandleX
        with get() = this.HandleWidth - 2.0
    member this.SmallHandleY
        with get() = this.HandleHeight - 2.0

    member this.TextAngle
        with get() = this.ArcStart * -1.0

    member this.RingSize
        with get() = this.SliderSize - this.SliderMargin * 2.0
    
    member this.CenterPoint
        with get() = Point(this.SliderSize/2.0 - this.SliderMargin, this.SliderSize/2.0 - this.SliderMargin)
    
    member this.RingOuterRadius
        with get() = this.RingSize / 2.0
    
    member this.RingInnerRadius
        with get() = this.RingOuterRadius - this.RingWidth
    
    member this.HandleRadius
        with get() = this.RingOuterRadius - this.RingWidth / 2.0


    member this.RingStop
        with get() =
            this.HandlePercent + 0.01

    member this.HandlePercent
        with get() =
            let perc =
                if this.Max <= this.Min then
                    0.0
                else
                    this.Value / (this.Max - this.Min)
            let adj = perc * (this.ArcSize / 360.0)
            
            Math.Clamp(adj, 0.0, 1.0)
            

    member this.HandlePosition
        with get() =
            // Convert handle percent -> radians -> x,y position at handle radius
            let radians = this.HandlePercent * 2.0 * System.Math.PI
            let center = this.SliderSize / 2.0
            let x = center + this.HandleRadius * sin(radians)
            let y = center - this.HandleRadius * cos(radians) // Y increases downwards
            // Offset from ring width center is half handle size
            let offsetX = this.HandleWidth / 2.0 
            let offsetY = this.HandleHeight / 2.0
            let adjX = x - offsetX 
            let adjY = y - offsetY 
            Point(adjX,adjY)

    // Overrides
    override this.OnApplyTemplate(e) =
        base.OnApplyTemplate(e)
        // Set control canvas for relative positioning
        sliderCanvas <- e.NameScope.Find("SliderCanvas") :?> Canvas

    override this.OnPointerPressed(e: PointerPressedEventArgs) =
        base.OnPointerPressed(e)
        this.OnDragStarted(e)
    
    override this.OnPointerReleased(e: PointerReleasedEventArgs) =
        base.OnPointerReleased(e)
        this.OnDragStopped(e)
    
    override this.OnPointerMoved(e: PointerEventArgs) =
        base.OnPointerMoved(e)
        this.OnDragged(e)
        
    override this.OnPropertyChanged(e: AvaloniaPropertyChangedEventArgs) =
        base.OnPropertyChanged(e)
        
        match e.Property.Name with
        | "Value" -> 
            let value = this.GetValue(ValueProperty)
            this.Value <- value
            ()
        | _ -> ()

    member this.OnDragStarted(e: PointerPressedEventArgs) =
        let pos = e.GetPosition(sliderCanvas)
        isDragging <- true
        this.SetValueFromPoint(pos) |> ignore
        ()

    member this.OnDragged(e: PointerEventArgs) =
        if isDragging then
            this.SetValueFromPoint(e.GetPosition(sliderCanvas)) |> ignore

        ()
    
    member this.OnDragStopped(e: PointerReleasedEventArgs) =
        isDragging <- false
        ()
    
    member private this.SetValueFromPoint(pos : Point) =
        let rads = this.PointToRadians (Point(this.SliderSize/2.0, this.SliderSize/2.0)) pos
        let rotated = this.ApplyRotation rads
        let percent = rotated / (this.ArcSize * Math.PI / 180.0)
        let value = percent * (this.Max - this.Min)
        let stepValue = Math.Ceiling(value / this.Step) * this.Step
        let clampValue = Math.Clamp(stepValue, this.Min, this.Max)
        // If rads > arc size, clamp to max
        let clampMax =
            if 2.0 * rads * (180.0 / Math.PI) > this.ArcSize then 
                this.Max
            else
                clampValue

        this.Value <- clampMax
        ()

    member private this.PointToRadians(center : Point) (pos : Point) =
        let dx = pos.X - center.X
        let dy = pos.Y - center.Y
        Math.Atan2(dy, dx)
    
    member private this.ApplyRotation(radians : double) =
        let startRotation = this.ArcStart * Math.PI / 180.0
        let rotated = radians + startRotation
        let rotatedresult =
            match this.ArcStart with
            | x when x = 0.00 || x = 360.0 -> rotated + (Math.PI / 2.0) // change base origin to top
            | _ -> rotated
        // Adjust the range to 0 - 2*pi
        if rotatedresult < 0.0 then rotatedresult + this.ArcSize * Math.PI / 180.0 else rotatedresult