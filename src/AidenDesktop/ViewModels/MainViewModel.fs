namespace AidenDesktop.ViewModels

open ReactiveElmish
open ReactiveElmish.Avalonia
open FluentAvalonia.UI.Controls
open FluentAvalonia.FluentIcons
open ReactiveUI
open Avalonia.Media
open System.Threading.Tasks
open App
open Avalonia.Layout


type NavItem() =
    (* NOTE: FluentAvalonia.NavigationView Icons support:
            BitmapIconSource, PathIconSource, and SymbolIconSource 
            It DOES NOT SUPPORT FontIconSource... >< 
        As Well: Icon size is inflexible at the moment, with talk of adding a resource: https://github.com/microsoft/microsoft-ui-xaml/issues/1710

            
            *)
    (* TODO: If we want multiple icon type support, a converter may be a better option
        at the moment, it can be only one type, so for the short term we'll have to choose one
     *)
    let createTestIcon() =
        (* let symbolIcon = SymbolIconSource()
        symbolIcon.Symbol <- Symbol.Home
        symbolIcon *)

        (* let pathIcon = PathIconSource()
        pathIcon.Data <- Geometry.Parse("M0,0 L0,1 1,1 1,0 z M0.5,0.5 L0.5,0 L0,0.5 L0.5,1 L1,0.5 L0.5,0 z")
        pathIcon *)

        let i = BitmapIconSource()
        i.UriSource <- System.Uri("avares://AidenDesktop/Assets/test.png")
        i
        
    let createBadge() =
        let b = InfoBadge()
        b.Value <- 0
        b.FontSize <- 8.0
        b.Foreground <- SolidColorBrush(Colors.White)
        b.Background <- SolidColorBrush(Colors.Orange)
        b.HorizontalAlignment <- HorizontalAlignment.Left
        b.VerticalAlignment <- VerticalAlignment.Top
        b.IsVisible <- false
        b

    let mutable _testIcon  = createTestIcon()
    let mutable _badge = createBadge()
    member val Name = "" with get, set
    member val Badge = _badge with get, set
    
    member this.Icon
        with get() = _testIcon
        and set(value) = _testIcon <- value

    member this.SetBadgeValue(value : int) =
        this.Badge.Value <- value
        if value > 0 then
            this.Badge.IsVisible <- true
        else
            this.Badge.IsVisible <- false
        
    
    new(name : string, icon : string) as self =
        NavItem() then
        do
            self.Name <- name
            let i = BitmapIconSource()
            i.UriSource <- System.Uri(sprintf "avares://AidenDesktop/Assets/%s.png" icon)
            self.Icon <- i
    
    new(name: string, icon: string, badgeValue: int) as self =
        NavItem() then
        do
            self.Name <- name
            let i = BitmapIconSource()
            i.UriSource <- System.Uri(sprintf "avares://AidenDesktop/Assets/%s.png" icon)
            self.Icon <- i
            self.SetBadgeValue(badgeValue)
            
(*     new(name : string, icon : Symbol) as self =
        NavItem() then
        do
            self.Name <- name
            self.Icon <- SymbolIconSource()
            self.Icon.Symbol <- icon *)
    

            

(* type NavItem =
    {
        Name: string
        Icon: string
    } *)

type MainViewModel(root: CompositionRoot) as self =
    inherit ReactiveElmishViewModel()
    
    let mutable _selectedNavItem : NavItem = NavItem("Home", "Home")
    

    
    let itemInvokedCommand : ReactiveCommand<NavigationViewItem, System.Reactive.Unit> =
        ReactiveCommand.CreateFromTask<NavigationViewItem>(self.Show)


    member self.ItemInvokedCommand with get() = itemInvokedCommand

    member self.ChatView = root.GetView<ChatViewModel>()
    member self.ContentView =
        self.BindOnChanged (app, _.View, fun m ->
            match m.View with
            | CounterView -> root.GetView<CounterViewModel>()
            | DoughnutView -> root.GetView<DoughnutViewModel>()
            | ChartView -> root.GetView<ChartViewModel>()
            | FilePickerView -> root.GetView<FilePickerViewModel>()
            | AboutView -> root.GetView<AboutViewModel>()
            | HomeView -> root.GetView<HomeViewModel>()
        )

    member this.SelectedNavItem
        with get() = _selectedNavItem
        and set(value) =
            _selectedNavItem <- value
            match value.Name with
            | "Counter" -> app.Dispatch (SetView CounterView)
            | "Chart" -> app.Dispatch (SetView ChartView)
            | "Dashboard" -> app.Dispatch (SetView DoughnutView)
            | "File Picker" -> app.Dispatch (SetView FilePickerView)
            | "About" -> app.Dispatch (SetView AboutView)
            | "Home" -> app.Dispatch (SetView HomeView)
            | _ -> ()
            
    member this.TestList = [ 
        NavItem("Home", "Home")
        NavItem("Counter", "Counter")
        NavItem("Chart", "Chart")
        NavItem("Dashboard", "Dashboard", 2)
        NavItem("File Picker", "Chat")
        NavItem("About", "About")
    ]
  
    member self.NavigationViewItems =
        [
            NavigationViewItem(Content = "Home", Tag = "HomeViewModel" )
            NavigationViewItem(Content = "Basic Counter", Tag = "CounterViewModel" )
            NavigationViewItem(Content = "Time Series", Tag = "ChartViewModel")
            NavigationViewItem(Content = "Dashboard", Tag = "DoughnutViewModel")
            NavigationViewItem(Content = "File Picker", Tag = "FilePickerViewModel")
            NavigationViewItem(Content = "About", Tag = "AboutViewModel")
        ]

    member self.Show(item: NavigationViewItem) =
        match item.Tag with
        | :? string as tag ->
            match tag with
            | "CounterViewModel" -> app.Dispatch (SetView CounterView)
            | "ChartViewModel" -> app.Dispatch (SetView ChartView)
            | "DoughnutViewModel" -> app.Dispatch (SetView DoughnutView)
            | "FilePickerViewModel" -> app.Dispatch (SetView FilePickerView)
            | "AboutViewModel" -> app.Dispatch (SetView AboutView)
            | "HomeViewModel" -> app.Dispatch (SetView HomeView)
            | _ -> ()
        | _ -> ()
        Task.CompletedTask

    member this.ShowAbout() = 
        printfn "Show About Called"
        app.Dispatch (SetView AboutView)
        
    

    static member DesignVM = new MainViewModel(Design.stub)