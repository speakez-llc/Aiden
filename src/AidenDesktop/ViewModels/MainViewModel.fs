namespace AidenDesktop.ViewModels

open ReactiveElmish
open ReactiveElmish.Avalonia
open App

type MainViewModel(root: CompositionRoot) =
    inherit ReactiveElmishViewModel()
    
    member this.ChatView = root.GetView<ChatViewModel>() 
    member this.ContentView = 
        this.BindOnChanged (app, _.View, fun m -> 
            match m.View with
            | CounterView -> root.GetView<CounterViewModel>()
            | DoughnutView -> root.GetView<DoughnutViewModel>()
            | ChartView -> root.GetView<ChartViewModel>()
            | FilePickerView -> root.GetView<FilePickerViewModel>()
            | DashboardView -> root.GetView<DashboardViewModel>()
            | AboutView -> root.GetView<AboutViewModel>()
        )

    member this.ShowChart() = app.Dispatch (SetView ChartView)
    member this.ShowDoughnut() = app.Dispatch (SetView DoughnutView)
    member this.ShowDashboard() = app.Dispatch (SetView DashboardView)
    member this.ShowCounter() = app.Dispatch (SetView CounterView)
    member this.ShowAbout() = app.Dispatch (SetView AboutView)
    member this.ShowFilePicker() = app.Dispatch (SetView FilePickerView)

    static member DesignVM = new MainViewModel(Design.stub)