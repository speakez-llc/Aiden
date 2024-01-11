namespace AidenDesktop.Views

open System.Diagnostics
open Avalonia
open Avalonia.VisualTree
open Avalonia.Controls
open Avalonia.Markup.Xaml
open Avalonia.Threading
open AidenDesktop.ViewModels
open System.Collections.Specialized

type ChatView() as this =
    inherit UserControl ()

    let mutable viewModel: ChatViewModel option = None

    do
        this.InitializeComponent()
        this.DataContextChanged.Add(fun args ->
            printfn $"DataContextChanged: {args}"
            
            //listBox.PropertyChanged.AddHandler(AvaloniaPropertyChangedEventHandler(this.ScrollToBottomSmooth) |> ignore

            viewModel <- this.DataContext :?> ChatViewModel |> Some
            match viewModel with
            | Some viewModel ->
                let listBox = this.FindControl<ListBox>("ChatWindow")
                listBox.GetPropertyChangedObservable(ListBox.BoundsProperty)
                |> Observable.subscribe (fun _ -> this.ScrollToBottomSmooth()) |> ignore

                viewModel.NewMessageEvent
                |> Observable.subscribe (fun _ ->
                    printfn "Scrolling to bottom"
                    this.ScrollToBottomSmooth())
                |> ignore
                (* viewModel.NewMessageEvent
                |> Observable.subscribe (fun _ ->
                    printfn "Scrolling to bottom"
                    this.ScrollToBottomSmooth())
                |> ignore *)
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
    

    (* member private this.ScrollToBottomSmooth() =
        printfn "ScrollToBottomSmooth"
        let listBox = this.FindControl<ListBox>("ChatWindow")
        if listBox.ItemCount > 0 then
            let item = listBox.ContainerFromIndex(listBox.ItemCount - 1)
            let target = (item :?> ListBoxItem).Bounds.Y
            let scrollViewerOption = listBox.GetVisualDescendants() |> Seq.tryFind (fun v -> v :? ScrollViewer) |> Option.map (fun v -> v :?> ScrollViewer)
            match scrollViewerOption with
            | Some scrollViewer ->
                //scrollViewer.Offset <- new Vector(scrollViewer.Offset.X, target)
                let sw = Stopwatch.StartNew()
                let timer = new DispatcherTimer(DispatcherPriority.Render)
                let start = scrollViewer.Offset.Y
                let diff = target - start
                timer.Tick.Add(fun _ ->
                    let elapsed = sw.Elapsed.TotalMilliseconds
                    if elapsed < 500.0 then
                        let offset = start + diff * (elapsed / 500.0)
                        scrollViewer.Offset <- new Vector(scrollViewer.Offset.X, offset)
                    else
                        timer.Stop()
                        scrollViewer.Offset <- new Vector(scrollViewer.Offset.X, target)
                )
                timer.Start()
            | None -> ()
*)
    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)