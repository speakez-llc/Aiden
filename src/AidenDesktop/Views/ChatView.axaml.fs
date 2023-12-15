namespace AidenDesktop.Views

open System.Threading.Tasks
open Avalonia.Controls
open Avalonia.Markup.Xaml
open AidenDesktop.ViewModels

type ChatView() as this =
    inherit UserControl ()
    
    let vm = new ChatViewModel()
    
    let scrollToEnd (listBox: ListBox) = 
       let itemCount = listBox.ItemCount
       if itemCount > 0 then 
           listBox.ScrollIntoView(listBox.Items |> Seq.item (itemCount - 1))

    let asyncScrollToEnd listBox =
        async {
            do! Task.Delay(100) |> Async.AwaitTask
            scrollToEnd listBox
        } |> Async.StartImmediate

    do
        AvaloniaXamlLoader.Load this
        this.DataContext <- vm
        // ViewModel and ListBox references
        let listBox = this.FindControl<ListBox>("ChatWindow")
        // Call asyncScrollToEnd when ListBox DataContext is changed
        listBox.DataContextChanged.Add(fun _ ->
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync((fun () -> asyncScrollToEnd listBox |> ignore), Avalonia.Threading.DispatcherPriority.Render) |> ignore)
        // Respond to any changes in the underlying collection
        vm.MessagesView.CollectionChanged.Add(fun _ -> asyncScrollToEnd listBox |> ignore)