namespace AidenDesktop.ViewModels

open Elmish
open ReactiveElmish
open ReactiveElmish.Avalonia
open DynamicData
open System.Collections.ObjectModel
open System

module Chat =
    type Message = { User: string; Text: string; Alignment: string; Color: string; BorderColor: string }

    type Model = { Messages: SourceList<Message> }

    type Msg =
        | SendMessage of string

    let init() =
        let initialMessages =
            [
                { User = "Aiden"; Text = "Anomaly detected in in-bound data. It mirrors a previous probe attack that has preceded a DDoS cycle by 20 minutes."
                  Alignment = "Left"; Color = "WhiteSmoke"; BorderColor = "Orange"  }
                { User = "Me"; Text = "Thanks I see it. Clear the alarm. Is there any news on the wire that this is hitting more than us?"
                  Alignment = "Right"; Color = "White"; BorderColor = "MidnightBlue"  }
                { User = "Aiden"; Text = "No headlines have been found in a recent sweep of known sources."
                  Alignment = "Left"; Color = "WhiteSmoke"; BorderColor = "Gray"  }
                { User = "Me"; Text = "OK. I have issued a request to re-route traffic. You can revert the status to green."
                  Alignment = "Right"; Color = "White"; BorderColor = "MidnightBlue"  }
                { User = "Aiden"; Text = "Thank you. I will continue to scan for news and monitor and notify on any DDoS activity."
                  Alignment = "Left"; Color = "WhiteSmoke"; BorderColor = "Gray"  }
            ]
        { Messages = SourceList.createFrom initialMessages}

    let update (msg: Msg) (model: Model) =
        match msg with
        | SendMessage text ->
            {
                Messages = model.Messages |> SourceList.add { User = "Me"; Text = text; Alignment = "Right"; Color = "White"; BorderColor = "MidnightBlue"  }
            }

open Chat

type ChatViewModel() as this =
    inherit ReactiveElmishViewModel()

    let local =
        Program.mkAvaloniaSimple init update
        |> Program.withErrorHandler (fun (_, ex) -> printfn "Error: %s" ex.Message)
        |> Program.mkStore
    let messages = ObservableCollection<Message>(this.BindSourceList(local.Model.Messages))

    member x.LastMessage
        with get() = Seq.last x.MessagesView
    member this.MessagesView: ObservableCollection<Message> = messages
    
    member this.SendMessage(message: string) =
        local.Dispatch (SendMessage message)

    static member DesignVM = new ChatViewModel()
