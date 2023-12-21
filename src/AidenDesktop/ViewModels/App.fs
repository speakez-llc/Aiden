module App

open Elmish
open ReactiveElmish.Avalonia
open Avalonia.Data.Converters

type Model =  
    { 
        View: View
        IsHomeViewActive: bool
    }

and View = 
    | CounterView
    | ChartView
    | DoughnutView
    | AboutView
    | FilePickerView
    | DashboardView
    | HomeView

type Msg = 
    | SetView of View
    | GoHome
    | SetHomeViewActive of bool

let init () = 
    { 
        View = HomeView
        IsHomeViewActive = true 
    }

let update (msg: Msg) (model: Model) =
    match msg with
    | SetView view -> 
        let isHomeViewActive = (view = HomeView)
        { model with View = view; IsHomeViewActive = isHomeViewActive }
    | GoHome -> { model with View = HomeView; IsHomeViewActive = true }
    | SetHomeViewActive isActive -> { model with IsHomeViewActive = isActive }


let app = 
    Program.mkAvaloniaSimple init update
    |> Program.withErrorHandler (fun (_, ex) -> printfn $"Error: {ex.Message}")
    //|> Program.withConsoleTrace
    |> Program.mkStore
