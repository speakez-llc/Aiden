namespace AidenDesktop.Views

open System
open Avalonia
open Avalonia.Input
open Avalonia.VisualTree
open Avalonia.Controls
open Avalonia.Markup.Xaml
open AidenDesktop.ViewModels

type ChatView() as this =
    inherit UserControl ()

    let mutable viewModel: ChatViewModel option = None

    do
        this.InitializeComponent()
        this.DataContextChanged.Add(fun _ ->
            viewModel <- this.DataContext :?> ChatViewModel |> Some
            match viewModel with
            | Some viewModel ->
                let listBox = this.FindControl<ListBox>("ChatWindow")
                listBox.GetPropertyChangedObservable(ListBox.BoundsProperty)
                |> Observable.subscribe (fun _ -> this.ScrollToBottomSmooth()) |> ignore

                viewModel.NewMessageEvent
                |> Observable.subscribe (fun _ ->
                    this.ScrollToBottomSmooth())
                |> ignore
            | None -> ()
            )

    member private this.ScrollToBottomSmooth() =
        let listBox = this.FindControl<ListBox>("ChatWindow")
        if listBox.ItemCount > 0 then
            let item = listBox.ContainerFromIndex(listBox.ItemCount - 1)
            if not (isNull item) then
                let target = (item :?> ListBoxItem).Bounds.Bottom
                let scrollViewerOption = listBox.GetVisualDescendants() |> Seq.tryFind (fun v -> v :? ScrollViewer) |> Option.map (fun v -> v :?> ScrollViewer)
                match scrollViewerOption with
                | Some scrollViewer ->
                    let targetOffset = target - scrollViewer.Bounds.Height
                    if targetOffset > scrollViewer.Offset.Y then
                        scrollViewer.Offset <- new Vector(scrollViewer.Offset.X, targetOffset)
                | None -> ()

    member this.MessageTextBox_KeyDown(sender: obj, e: KeyEventArgs) =
        match e.Key with
        | Key.Enter when e.KeyModifiers = KeyModifiers.None ->
            let viewModel = this.DataContext :?> ChatViewModel
            if not (String.IsNullOrEmpty(viewModel.MessageText)) && not (String.IsNullOrWhiteSpace(viewModel.MessageText)) then
                // Invoke the SendMessage command of the ViewModel
                viewModel.SendMessage(viewModel.MessageText)
                e.Handled <- true
        | _ -> ()



    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)