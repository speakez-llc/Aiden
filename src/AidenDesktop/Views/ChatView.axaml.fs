namespace AidenDesktop.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml
open Avalonia.Threading
open AidenDesktop.ViewModels
open System

type ChatView() as this =
    inherit UserControl ()

    let mutable viewModel: ChatViewModel option = None

    do this.InitializeComponent()

    override this.OnDataContextChanged args =
        base.OnDataContextChanged args
        viewModel <- this.DataContext :?> ChatViewModel |> Some
        match viewModel with
        | Some viewModel ->
            viewModel.NewMessageEvent
            |> Observable.subscribe (fun _ -> this.ScrollToBottom())
            |> ignore
        | None -> ()

    member private this.ScrollToBottom() =
        let listBox = this.FindControl<ListBox>("ChatWindow")
        if listBox.ItemCount > 0 then
            let timer = new DispatcherTimer(DispatcherPriority.Loaded)
            timer.Interval <- TimeSpan.FromMilliseconds(100.0)
            timer.Tick.Subscribe(fun _ ->
                listBox.ScrollIntoView(listBox.Items[listBox.ItemCount - 1])
                timer.Stop())
            timer.Start()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)

