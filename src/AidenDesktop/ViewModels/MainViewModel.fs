namespace AidenDesktop.ViewModels

open ReactiveElmish
open ReactiveElmish.Avalonia
open FluentAvalonia.UI.Controls
open FluentAvalonia.FluentIcons
open ReactiveUI
open System.Threading.Tasks
open App

type NavItem =
    {
        Name: string
        Icon: FluentIconSymbol
    }

type MainViewModel(root: CompositionRoot) as self =
    inherit ReactiveElmishViewModel()
    
    let mutable _selectedNavItem : NavItem = { Name="Home"; Icon= FluentIconSymbol.Home24Regular } 
    
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
        )

    member this.SelectedNavItem
        with get() = _selectedNavItem
        and set(value) =
            _selectedNavItem <- value
            match value.Name with
            | "Home" -> app.Dispatch (SetView CounterView)
            | "Counter" -> app.Dispatch (SetView CounterView)
            | "Chart" -> app.Dispatch (SetView ChartView)
            | "Dashboard" -> app.Dispatch (SetView DoughnutView)
            | "File Picker" -> app.Dispatch (SetView FilePickerView)
            | "About" -> app.Dispatch (SetView AboutView)
            | _ -> ()
            
    member this.TestList = 
        let createNavItem name iconSymbol =
            let navItem = { Name = name; Icon = iconSymbol }
            navItem
        [ 
            createNavItem "Home" FluentIconSymbol.Home24Regular
            createNavItem "Counter" FluentIconSymbol.Calculator24Regular
            createNavItem "Chart" FluentIconSymbol.ChartMultiple24Regular
            createNavItem "Dashboard" FluentIconSymbol.ViewDesktop24Regular
            createNavItem "File Picker" FluentIconSymbol.Folder24Regular
            createNavItem "About" FluentIconSymbol.Info24Regular
        ]
  
    member self.NavigationViewItems =
        [
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
            | _ -> ()
        | _ -> ()
        Task.CompletedTask

    member this.ShowAbout() = 
        printfn "Show About Called"
        app.Dispatch (SetView AboutView)

    static member DesignVM = new MainViewModel(Design.stub)