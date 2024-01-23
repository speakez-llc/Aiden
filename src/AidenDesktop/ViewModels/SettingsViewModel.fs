namespace AidenDesktop.ViewModels

open ReactiveElmish
open App

type SettingsViewModel() =
    inherit ReactiveElmishViewModel()

    member this.Version = "v1.0"
    member this.Ok() = app.Dispatch GoHome

    static member DesignVM = 
        new SettingsViewModel()