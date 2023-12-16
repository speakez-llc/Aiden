namespace AidenDesktop

open Microsoft.Extensions.DependencyInjection
open AidenDesktop.ViewModels
open AidenDesktop.Views
open ReactiveElmish.Avalonia
open Avalonia
open Avalonia.Data.Converters

type StringToMarginConverter() =
    interface IValueConverter with
        member _.Convert(value : obj, targetType : System.Type, parameter : obj, culture : System.Globalization.CultureInfo) : obj =
            match value with
            | :? string as alignment ->
                if alignment = "Left" then new Thickness(10.0, 5.0, 50.0, 5.0)
                else new Thickness(50.0, 5.0, 10.0, 5.0)
            | _ -> new Thickness(0.0)
        member _.ConvertBack(value : obj, targetType : System.Type, parameter : obj, culture : System.Globalization.CultureInfo) : obj =
            null

type AppCompositionRoot() =
    inherit CompositionRoot()

    let mainView = MainView()

    override this.RegisterServices services = 
        base.RegisterServices(services)
            .AddSingleton<FileService>(FileService(mainView))

    override this.RegisterViews() = 
        Map [
            VM.Key<MainViewModel>(), View.Singleton(mainView)
            VM.Key<ChatViewModel>(), View.Singleton<ChatView>()
            VM.Key<CounterViewModel>(), View.Singleton<CounterView>()
            VM.Key<AboutViewModel>(), View.Singleton<AboutView>()
            VM.Key<ChartViewModel>(), View.Transient<ChartView>()
            VM.Key<DoughnutViewModel>(), View.Transient<DoughnutView>()
            VM.Key<FilePickerViewModel>(), View.Singleton<FilePickerView>()
            VM.Key<DashboardViewModel>(), View.Transient<DashboardView>()
        ]
        
