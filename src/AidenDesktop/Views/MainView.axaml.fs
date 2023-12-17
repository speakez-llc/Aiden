namespace AidenDesktop.Views

open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml
open FluentAvalonia.UI.Windowing

type MainView () as this = 
    inherit AppWindow ()

    do this.InitializeComponent()

    member private this.InitializeComponent() =
#if DEBUG
        this.AttachDevTools()
#endif
        AvaloniaXamlLoader.Load(this)
