namespace AidenDesktop.Views

open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml

type ZoomView () as this = 
    inherit UserControl ()

    do this.InitializeComponent()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)