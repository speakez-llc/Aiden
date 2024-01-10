namespace AidenDesktop


open Microsoft.Extensions.DependencyInjection
open AidenDesktop.ViewModels
open AidenDesktop.Views
open ReactiveElmish.Avalonia

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
            VM.Key<ZoomViewModel>(), View.Transient<ZoomView>()
            VM.Key<FilePickerViewModel>(), View.Singleton<FilePickerView>()
            VM.Key<DashboardViewModel>(), View.Transient<DashboardView>()
            VM.Key<HomeViewModel>(), View.Singleton<HomeView>()
        ]
        
