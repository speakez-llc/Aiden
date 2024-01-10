namespace AidenDesktop.Views

open System
open Avalonia.Controls
open Avalonia.Markup.Xaml

type DashboardView() as this =
    inherit UserControl()

    do
        this.InitializeComponent()
    

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this) |> ignore
