namespace AidenDesktop.ViewModels

open Elmish
open ReactiveElmish
open ReactiveElmish.Avalonia
open DynamicData
open System
open Avalonia.Data.Converters

module Chat =
    type Message = { User: string; Text: string; Alignment: string; Color: string; BorderColor: string; IsMe: bool }

    type Model = { Messages: SourceList<Message> }

    type Msg =
        | SendMessage of string
        | SendAidenMessage of string
        | FeedMessage of string * int

    let init() =
        let initialMessages =
            [
                { User = "Aiden"; Text = "There are signals of low frequency probes at two customer sites in the past 15 minutes. It's likely a precursor, as those patterns have been observed before larger attacks."
                  Alignment = "Left"; Color = "Glaucous"; BorderColor = "Gray" ; IsMe = false }
                { User = "Houston"; Text = "Continue to monitor and if traffic patterns change or if the probes spread, notify me here."
                  Alignment = "Right"; Color = "Glaucous"; BorderColor = "Blue" ; IsMe = true }
                { User = "Aiden"; Text = "Probes are registering at two more customer sites, but total traffic volume is within tolerance."
                  Alignment = "Left"; Color = "Glaucous"; BorderColor = "Orange" ; IsMe = false }
                { User = "Aiden"; Text = "Countries of origin and source IP subnets are a high-confidence match to two attacks in the last three months."
                  Alignment = "Left"; Color = "Glaucous"; BorderColor = "Orange" ; IsMe = false }
            ]
        { Messages = SourceList.createFrom initialMessages}

    let update (msg: Msg) (model: Model) =
        match msg with
        | SendMessage text ->
            let msg = { User = "Houston"; Text = text; Alignment = "Right"; Color = "White"; BorderColor = "MidnightBlue" ; IsMe  = true }
            // printfn "Message: %A" msg
            {                
                Messages = model.Messages |> SourceList.add msg
            }
        | SendAidenMessage text ->
            let msg = { User = "Aiden"; Text = text; Alignment = "Left"; Color = "Glaucous"; BorderColor = "Orange" ; IsMe = false }
            {                
                Messages = model.Messages |> SourceList.add msg
            }
        | FeedMessage (text, index) ->
            let messages = model.Messages
            let msg = { User = "Aiden"; Text = text; Alignment = "Left"; Color = "Glaucous"; BorderColor = "Orange" ; IsMe = false }
            messages.ReplaceAt (index, msg)
            { model with Messages = messages }
            
open Chat

type ChatViewModel() =
    inherit ReactiveElmishViewModel()

    let newMessageEvent = new Event<_>()

    let local =
        Program.mkAvaloniaSimple init update
        |> Program.withErrorHandler (fun (_, ex) -> printfn "Error: %s" ex.Message)
        |> Program.mkStore

    do
        // Subscribe to the Messages list's Changed event
        local.Model.Messages.Connect()
            .Subscribe(fun _ -> 
                newMessageEvent.Trigger())
            |> ignore

    member this.MessagesView = this.BindSourceList(local.Model.Messages)

    member this.NewMessageEvent = newMessageEvent.Publish

    member this.SendMessage(message: string) =
        local.Dispatch (SendMessage message)
        //this.FeedMessage(message)
        // Create an async task that waits for a random amount of time and then sends a FeedMessage
        let responseTask = async {
            let waitTime = Random().Next(1000, 2000) 
            do! Async.Sleep waitTime
            this.FeedMessage ("This is a very long test message that plays out word by word which is a very useful thing for being able to eventually interrupt a generated message that goes on for too long.")
        }

        // Start the async task
        Async.StartImmediate(responseTask)

    
    member this.FeedMessage(message: string) =
        // Break up message into chunks and deliver at slightly varied cadence
        let index = local.Model.Messages.Count
        let len = message.Length
        let mutable i = 0
        let mutable waitTime = 0
        let mutable fullMessage = ""
        let updateFeed(msg : string) (wait : int) =
            async {
                    do! Async.Sleep (wait)                    
                    //printfn $"Feeding message: {msg}"
                    local.Dispatch (FeedMessage (msg, index))
                } |> Async.StartImmediate

        while i < len do
            let chunkSize = Random().Next(8, 12)
            let chunk = message.Substring(i, Math.Min(chunkSize, len - i))
            i <- i + chunkSize
            // NOTE: Due to the lack of a get for SourceList, we have to maintain the memory here
            fullMessage <- fullMessage + chunk
            if fullMessage = chunk then
                //printfn $"Sending message: {fullMessage}"
                local.Dispatch (SendAidenMessage fullMessage)
            else
                waitTime <- waitTime + Random().Next(100, 300)
                updateFeed(fullMessage) (waitTime)
                
   


    static member DesignVM = new ChatViewModel()
