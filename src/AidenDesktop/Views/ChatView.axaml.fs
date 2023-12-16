namespace AidenDesktop.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml

type ChatView() as this =
    inherit UserControl ()
    
    do this.InitializeComponent()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)