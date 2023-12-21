namespace AidenDesktop.ViewModels

open Elmish
open ReactiveElmish
open ReactiveElmish.Avalonia
open FluentAvalonia.UI.Controls
open FluentAvalonia.FluentIcons
open ReactiveUI
open Avalonia.Media
open System.Threading.Tasks
open App
open Avalonia.Layout


// TODO: Move to a shared module - I have a Models folder in my branch for base types, which is where I'd put this... thoughts?
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

        let i = new BitmapIconSource()
        i.UriSource <- System.Uri("avares://AidenDesktop/Assets/test.png")
        i
        
    let createBadge() =
        let b = InfoBadge()
        b.Value <- 0
        b.FontSize <- 8.0
        b.Foreground <- SolidColorBrush(Colors.White)
        b.Background <- SolidColorBrush(Colors.DarkOrange)
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
            let i = new BitmapIconSource()
            i.UriSource <- System.Uri(sprintf "avares://AidenDesktop/Assets/%s.png" icon)
            self.Icon <- i
    
    new(name: string, icon: string, badgeValue: int) as self =
        NavItem() then
        do
            self.Name <- name
            let i = new BitmapIconSource()
            i.UriSource <- System.Uri(sprintf "avares://AidenDesktop/Assets/%s.png" icon)
            self.Icon <- i
            self.SetBadgeValue(badgeValue)
            
(*     new(name : string, icon : Symbol) as self =
        NavItem() then
        do
            self.Name <- name
            self.Icon <- SymbolIconSource()
            self.Icon.Symbol <- icon *)

module MainViewModule =

    type Model =
        {
            ChatOpen: bool
            ChatAlertCount: int
            ShowChatBadge: bool
            SelectedNavItem: NavItem
            NavigationList: NavItem list
        }
    
    type Msg =
    | ToggleChat of bool
    | SetChatAlertCount of int
    | SelectedNavItemChanged of NavItem

    let init() = 
        { 
            ChatOpen = false 
            ChatAlertCount = 2
            ShowChatBadge = true
            SelectedNavItem = NavItem("Home", "Home")
            NavigationList = [ 
                NavItem("Home", "FA_Home")
                NavItem("Counter", "FA_Counter")
                NavItem("Chart", "FA_Chart")
                NavItem("Dashboard", "FA_Map", 2)
                NavItem("File Picker", "FA_File")
                NavItem("About", "FA_Info")
            ]
        }

    let update (msg: Msg) (model: Model) =
        match msg with
        | ToggleChat b -> 
            // Clear chat badge on close
            if b = false then
                for item in model.NavigationList do
                    item.SetBadgeValue(0)
                { model with ChatOpen = b; ChatAlertCount = 0; ShowChatBadge = false }
            else
                { model with ChatOpen = b }
        | SetChatAlertCount count ->
            // Set badge as active
            { model with ChatAlertCount = count }
        | SelectedNavItemChanged item ->
            match item.Name with
            | "Counter" -> app.Dispatch (SetView CounterView)
            | "Chart" -> app.Dispatch (SetView ChartView)
            | "Dashboard" -> app.Dispatch (SetView DoughnutView)
            | "File Picker" -> app.Dispatch (SetView FilePickerView)
            | "About" -> app.Dispatch (SetView AboutView)
            | "Home" -> app.Dispatch (SetView HomeView)
            | _ -> ()            
            { model with SelectedNavItem = item }
    
open MainViewModule


    


type MainViewModel(root: CompositionRoot) =
    inherit ReactiveElmishViewModel()
    
    let local =
        Program.mkAvaloniaSimple init update
        |> Program.withErrorHandler (fun (_, ex) -> printfn "Error: %s" ex.Message)
        |> Program.mkStore
    

    member self.ChatOpen
        with get() = self.Bind(local, _.ChatOpen)
        and set(value) = local.Dispatch (ToggleChat value)
    
    member self.ChatAlertCount
        with get() = self.Bind(local, _.ChatAlertCount)
        and set(value) = local.Dispatch (SetChatAlertCount value)
    
    member self.ShowChatBadge
        with get() = self.Bind(local, _.ShowChatBadge)

    member self.SelectedNavItem
        with get() = self.Bind(local, _.SelectedNavItem)
        and set(value) = local.Dispatch (SelectedNavItemChanged value)

    member self.NavigationList = self.Bind(local, _.NavigationList)

    member self.ChatView = root.GetView<ChatViewModel>()
    member self.ContentView =
        self.BindOnChanged (app, _.View, fun m ->
            match m.View with
            | CounterView -> root.GetView<CounterViewModel>()
            | DoughnutView -> root.GetView<DoughnutViewModel>()
            | ChartView -> root.GetView<ChartViewModel>()
            | FilePickerView -> root.GetView<FilePickerViewModel>()
            | DashboardView -> root.GetView<DashboardViewModel>()
            | AboutView -> root.GetView<AboutViewModel>()
            | HomeView -> root.GetView<HomeViewModel>()
        )


    static member DesignVM = new MainViewModel(Design.stub)