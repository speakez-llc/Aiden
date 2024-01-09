namespace AidenDesktop.ViewModels

open System
open System.Threading
open Elmish
open OllamaSharp.Models.Chat
open ReactiveElmish
open ReactiveElmish.Avalonia
open DynamicData
open OllamaSharp

module Chat =
    type ChatMessage = { User: string; Text: string; Alignment: string; Color: string; BorderColor: string; IsMe: bool }

    type Model = { Messages: SourceList<ChatMessage>; IsProcessing: bool; MessageText: string }

    type Msg =
        | SendMessage of string
        | SendAidenMessage
        | FeedMessage of string * int
        | StartProcessing
        | StopProcessing
        | ClearMessageText
        | CancelResponseStream
        
    let ollamaUri = Uri("http://aiden.speakez.dev:22161")
    let ollamaClient = OllamaApiClient(ollamaUri)
    ollamaClient.SelectedModel <- "llama2:latest"
    
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
        
        { Messages = SourceList.createFrom initialMessages; IsProcessing = false; MessageText = ""}

    let update (msg: Msg) (model: Model) =
        match msg with
        | SendMessage text ->
            let msg = { User = "Houston"; Text = text; Alignment = "Right"; Color = "White"; BorderColor = "MidnightBlue" ; IsMe  = true }
            printfn "Message: %A" msg
            {
                model with Messages = model.Messages |> SourceList.add msg
            }
        | SendAidenMessage ->
            printfn "Message: %A" msg
            let msg = { User = "Aiden"; Text = ""; Alignment = "Left"; Color = "Glaucous"; BorderColor = "Orange" ; IsMe = false }
            {                
                model with Messages = model.Messages |> SourceList.add msg
            }
        | FeedMessage (text, index) ->
            printfn "FeedMessage: %A" text
            let messages = model.Messages
            let msg = { User = "Aiden"; Text = text; Alignment = "Left"; Color = "Glaucous"; BorderColor = "Orange" ; IsMe = false }
            messages.ReplaceAt (index, msg)
            { model with Messages = messages }
        | StartProcessing ->
            printfn "StartProcessing"
            { model with IsProcessing = true }
        | StopProcessing ->
            printfn "StopProcessing"
            { model with IsProcessing = false }
        | ClearMessageText ->
            printfn "ClearMessageText"
            { model with MessageText = "" }
            
open Chat

type ChatViewModel() as this =
    inherit ReactiveElmishViewModel()
    let newMessageEvent = Event<_>()
    let handle = ollamaClient.Chat(Action<ChatResponseStream>(fun (stream: ChatResponseStream) -> 
        let message = stream.Message
        // printfn $"Received message: %s{message.Content}"
        this.FeedMessage (message.Content, 0)
    ))
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
    
    member this.MessageText = this.Bind(local, _.MessageText)
    member this.NewMessageEvent = newMessageEvent.Publish
    
    member this.IsProcessing = this.Bind(local, _.IsProcessing)
        
    member this.SendMessage(message: string) =
        try
            local.Dispatch (SendMessage message)
            local.Dispatch ClearMessageText
            let cts = new CancellationTokenSource() // For cancellation support

            let streamer = fun (stream: ChatResponseStream) ->
                async {
                    while not stream.Done do // Use stream.Done to control the loop
                        let messageChunk = stream.Message // Handle each word as a chunk
                        this.FeedMessage(messageChunk.Content, 0)
                } |> Async.StartImmediate

            let responseTask = async {
                local.Dispatch(StartProcessing)
                local.Dispatch(ClearMessageText)
                handle.Send(message) |> ignore
            }
            responseTask |> Async.StartAsTask |> ignore

        with
        | ex -> printfn $"Error in SendMessage: %s{ex.Message}"

    member this.FeedMessage(message: string * int) =
        try
            let messages = local.Model.Messages.Items |> List.ofSeq
            if (messages |> List.last).User <> "Aiden" then
                local.Dispatch(SendAidenMessage)
                local.Dispatch(StopProcessing)
            let token = message |> fst
            //printfn $"Streamed token: %s{token}"
            // get text from last message in SourceList and add the token to it
            let fullMessage = (messages |> List.ofSeq |> List.last).Text + token
            let updatedMsg = { User = "Aiden"; Text = fullMessage; Alignment = "Left"; Color = "Glaucous"; BorderColor = "Orange"; IsMe = false }

            local.Model.Messages.ReplaceAt(local.Model.Messages.Count - 1, updatedMsg)

        with
        | ex -> printfn $"Error in FeedMessage: %s{ex.Message}"
        
       
    static member DesignVM = new ChatViewModel()
