namespace AidenDesktop.ViewModels


open Avalonia
open Avalonia.Styling
open Elmish
open Avalonia.Media
open Avalonia.Layout
open ReactiveElmish
open ReactiveElmish.Avalonia
open FluentAvalonia.UI.Controls
open App


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
    let createBadge() =
        let b = InfoBadge()
        b.Value <- 0
        b.FontSize <- 8.0
        b.Foreground <- SolidColorBrush(Colors.White)
        b.Background <- SolidColorBrush(Colors.Navy)
        b.HorizontalAlignment <- HorizontalAlignment.Left
        b.VerticalAlignment <- VerticalAlignment.Top
        b.IsVisible <- false
        b

    
    let createIcon(iconKey: string) =
        let pathIcon = PathIconSource()
        let geometry = Application.Current.Resources.[iconKey] :?> Geometry
        pathIcon.Data <- geometry
        pathIcon
                
    let mutable _icon = createIcon("Home")
        
    let mutable _badge = createBadge()
    member val IconSource = _icon :> IconSource with get
    member val Icon = _icon with get, set

    member val Name = "" with get, set
    member val Badge = _badge with get, set
    

    member this.SetBadgeValue(value : int) =
        this.Badge.Value <- value
        if value > 0 then
            this.Badge.IsVisible <- true
        else
            this.Badge.IsVisible <- false
        
    
    new(name : string, iconKey : string) as self =
        NavItem() then
        do
            self.Name <- name
            let i = new PathIconSource()
            i.Data <- Application.Current.Resources.[iconKey] :?> Geometry
            self.Icon <- i
    
    new(name: string, iconKey: string, badgeValue: int) as self =
        NavItem() then
        do
            self.Name <- name
            let i = new PathIconSource()
            i.Data <- Application.Current.Resources.[iconKey] :?> Geometry
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
            IsDarkThemeEnabled: bool
        }
    
    type Msg =
    | ToggleChat of bool
    | SetChatAlertCount of int
    | SelectedNavItemChanged of NavItem
    | IsDarkThemeEnabled of bool
    | ToggleTheme of bool

    let init() = 
        { 
            ChatOpen = false 
            ChatAlertCount = 2
            ShowChatBadge = true
            SelectedNavItem = NavItem("Home", "Home")
            NavigationList = [ 
                NavItem("Home", "Home")
                NavItem("Line Chart", "Line", 2)
                NavItem("Map Dashboard", "Globe")
                NavItem("File Picker", "FileImport")
                NavItem("About", "Info")
            ]
            IsDarkThemeEnabled = true
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
            | "Home" -> app.Dispatch (SetView HomeView)
            | "Line Chart" -> app.Dispatch (SetView ChartView)
            | "Map Dashboard" -> app.Dispatch (SetView DashboardView)
            | "File Picker" -> app.Dispatch (SetView FilePickerView)
            | "About" -> app.Dispatch (SetView AboutView)
            | _ -> ()            
            { model with SelectedNavItem = item }
        | ToggleTheme t ->
            { model with IsDarkThemeEnabled = t } 
    
open MainViewModule

type MainViewModel(root: CompositionRoot) as self =
    inherit ReactiveElmishViewModel()

    let local =
        Program.mkAvaloniaSimple init update
        |> Program.withErrorHandler (fun (_, ex) -> printfn "Error: %s" ex.Message)
        |> Program.mkStore

    do
        self.PropertyChanged.Add(fun args ->
            if args.PropertyName = "IsDarkThemeEnabled" then
                self.SwitchTheme())

    member self.IsDarkThemeEnabled
        with get() = self.Bind(local, _.IsDarkThemeEnabled)
        and set(value) = local.Dispatch (ToggleTheme value)

    member this.SwitchTheme() =
        if this.IsDarkThemeEnabled then
            Application.Current.RequestedThemeVariant <- ThemeVariant.Dark
        else
            Application.Current.RequestedThemeVariant <- ThemeVariant.Light

    // Other code...

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
    
    member self.createIcon(iconKey: string) =
        let fontIcon = FontIconSource()
        fontIcon.FontFamily <- FontFamily("FontAwesome")
        fontIcon.Glyph <- iconKey
        fontIcon
      
    member self.ChatView = root.GetView<ChatViewModel>()
    member self.ContentView =
        self.BindOnChanged (app, _.View, fun m ->
            match m.View with
            | DoughnutView -> root.GetView<DoughnutViewModel>()
            | ChartView -> root.GetView<ChartViewModel>()
            | FilePickerView -> root.GetView<FilePickerViewModel>()
            | DashboardView -> root.GetView<DashboardViewModel>()
            | AboutView -> root.GetView<AboutViewModel>()
            | HomeView -> root.GetView<HomeViewModel>()
        )

    static member DesignVM = new MainViewModel(Design.stub)