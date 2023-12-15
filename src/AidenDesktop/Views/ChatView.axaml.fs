namespace AidenDesktop.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml
open AidenDesktop.ViewModels

type ChatView() as self =
    inherit UserControl ()

    let vm = new ChatViewModel()

    do
        AvaloniaXamlLoader.Load self
        self.DataContext <- vm