namespace AidenDesktop.ViewModels

open ReactiveElmish
open ReactiveElmish.Avalonia
open App
open FluentAvalonia.UI.Controls
open ReactiveUI
open System.Threading.Tasks

type MainViewModel(root: CompositionRoot) as self =
    inherit ReactiveElmishViewModel()
    
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

    member self.NavigationViewItems =
        [
            NavigationViewItem(Content = "Basic Counter", Tag = "CounterViewModel" )
            NavigationViewItem(Content = "Time Series", Tag = "ChartViewModel")
            NavigationViewItem(Content = "Map Dashboard", Tag = "DoughnutViewModel")
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

    static member DesignVM = new MainViewModel(Design.stub)