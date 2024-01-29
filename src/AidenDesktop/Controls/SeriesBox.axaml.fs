namespace AidenDesktop.Controls

open System
open System.Windows.Input
open System.ComponentModel
open System.Collections.ObjectModel
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.Media
open Avalonia.Input
open AidenDesktop.Models
open Avalonia.ReactiveUI


type SeriesBox () =
    inherit TemplatedControl ()

    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()

    static let FilterUpdatedProperty = AvaloniaProperty.Register<SeriesBox, ICommand>("FilterUpdated")
    static let SeriesNameProperty = StyledProperty.Register<SeriesBox, string>("SeriesName", "")
    static let SeriesListProperty = StyledProperty.Register<SeriesBox, ObservableCollection<SeriesData>>("SeriesList", ObservableCollection<SeriesData>())
    static let SeriesFilterProperty = StyledProperty.Register<SeriesBox, FilterItem List>("SeriesFilter", [])
    static let SeriesTypesProperty = StyledProperty.Register<SeriesBox, string list>("SeriesTypes", ["VPN"; "TOR"; "PRX"; "COO"])

    static let ChartTypesProperty = StyledProperty.Register<SeriesBox, string list>("ChartTypes", ["Pie"; "Bar"; "Line"; "Area"])

    static let SelectedChartTypeProperty = StyledProperty.Register<SeriesBox, EZChartType>("SelectedChartType", EZChartType.Pie)
    static let CanCloseProperty = StyledProperty.Register<SeriesBox, bool>("CanClose", false)

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChanged.Publish
    member this.add_PropertyChanged(handler) = propertyChanged.Publish.AddHandler(handler)
    member this.remove_PropertyChanged(handler) = propertyChanged.Publish.RemoveHandler(handler)

    member this.FilterUpdated
        with get() = this.GetValue(FilterUpdatedProperty)
        and set(value) =
            this.SetValue(FilterUpdatedProperty, value) |> ignore
    
    member this.SeriesName
        with get() = this.GetValue(SeriesNameProperty)
        and set(value) =
            this.SetValue(SeriesNameProperty, value) |> ignore
            this.NotifyPropertyChanged("SeriesName")

    member this.SeriesList
        with get() = this.GetValue(SeriesListProperty)
        and set(value) =
            this.SetValue(SeriesListProperty, value) |> ignore
            (* for item in this.SeriesList do
                printfn $"{item.Name} : {item.Count} : {item.Geography}" *)
    
    member this.SeriesFilter
        with get() = this.GetValue(SeriesFilterProperty)

            
    
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
    
    member this.CanClose
        with get() = this.GetValue(CanCloseProperty)
        and set(value) =
            this.SetValue(CanCloseProperty, value) |> ignore

    member this.BackEndX
        with get() = this.Width / 2.0
    
    member this.BackEndY
        with get() = this.Height / 2.0

    member this.ShowPie
        with get() = this.SelectedChartType = EZChartType.Pie
    member this.ShowGeo
        with get() = this.SelectedChartType = EZChartType.GeoMap
    
    member this.OnFilterChange (sender: obj) (args: SelectionChangedEventArgs) =
        
        // set Show on FilterItem by Added/Removed items
        for item in args.AddedItems do
            if this.FilterUpdated <> null then
                let filterItem = item :?> FilterItem
                this.FilterUpdated.Execute({SeriesName=this.SeriesName; FilterName=filterItem.Name; FilterStatus=true}) |> ignore
       
        for item in args.RemovedItems do
            if this.FilterUpdated <> null then
                let filterItem = item :?> FilterItem
                this.FilterUpdated.Execute({SeriesName=this.SeriesName; FilterName=filterItem.Name; FilterStatus=false}) |> ignore


    member private this.NotifyPropertyChanged(propertyName : string) =
        printfn $"SeriesBox NotifyPropertyChanged: {propertyName}"
        propertyChanged.Trigger(this, PropertyChangedEventArgs(propertyName))

    override this.OnApplyTemplate(e) =
        base.OnApplyTemplate(e)
        //printfn "SeriesBox OnApplyTemplate"
        (* for item in this.SeriesList do
            printfn $"{item.Name} : {item.Count} : {item.Geography}" *)
        
        // Initialize selected states for filter items
        let listbox = e.NameScope.Find<ListBox>("SeriesMaskListbox") |> Option.ofObj
        match listbox with
            | Some lb -> 
                lb.SelectionChanged.AddHandler(this.OnFilterChange)                
                for item in this.SeriesFilter do
                    if item.Show then lb.SelectedItems.Add(item) |> ignore                
            | None -> printfn $"Could not find {this.SeriesName} listbox"
        

    override this.OnPropertyChanged(e : AvaloniaPropertyChangedEventArgs) =
        base.OnPropertyChanged(e)
        //printfn $"SeriesBox OnPropertyChanged: {e.Property.Name}"