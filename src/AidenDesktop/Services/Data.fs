module Data

open System
open System.Data
open System.IO
open Akka.Actor
open Akkling
open Npgsql
open Microsoft.Extensions.Configuration

type DatabaseResponse =
    | Results of (string * int64) list
    | Error of string

type ChildMessage =
    | GetData of AsyncReplyChannel<DatabaseResponse>
    | Terminate
    
let mutable singletonDbConnection: NpgsqlConnection option = None

let configuration = 
    let builder = new ConfigurationBuilder()
    builder.SetBasePath(Directory.GetCurrentDirectory()) |> ignore
    builder.AddJsonFile("appsettings.json", optional = false, reloadOnChange = true) |> ignore
    builder.Build()
let connectionString = configuration.GetConnectionString("connectionString")
printfn "connectionString: %s" connectionString

let ensureDbConnection connectionString =
    match singletonDbConnection with
    | Some conn when conn.State <> ConnectionState.Closed 
                   && conn.State <> ConnectionState.Broken -> conn
    | _ ->
        match singletonDbConnection with
        | Some oldConn -> 
            if oldConn.State <> ConnectionState.Closed then
                oldConn.Close()
        | None -> ()

        let newConn = new NpgsqlConnection(connectionString)
        newConn.Open()
        singletonDbConnection <- Some newConn
        newConn
let system = System.create "AidenActors" <| Configuration.defaultConfig()
let childActor (dbConnection: NpgsqlConnection) (mailbox: Actor<_>) =
            
    let query = 
        @"SELECT UNNEST(string_to_array(vpn, ':')) AS vpn_part, COUNT(*) AS count
          FROM events_hourly
          WHERE event_time >= now() AT TIME ZONE 'UTC' - INTERVAL '1 minute'
          GROUP BY vpn_part;"
    
    let rec loop () = actor {
        let! msg = mailbox.Receive()
        match msg with
        | GetData replyChannel ->
            try
                use cmd = new NpgsqlCommand(query, dbConnection)
                use reader = cmd.ExecuteReader()
                let results = 
                    [ while reader.Read() do
                        yield (
                            reader.GetString(reader.GetOrdinal("vpn_part")),
                            reader.GetInt64(reader.GetOrdinal("count"))
                        ) ]
                replyChannel.Reply (Results results)
                ()
            with
            | ex -> 
                replyChannel.Reply (Error ex.Message)
                ()
        | Terminate ->
            if dbConnection.State = ConnectionState.Open then
                dbConnection.Close()
            Stop
        return! loop ()
    }
    loop ()

let databaseParentActor(system: ActorSystem) : ICancelable * IActorRef =
    let connectionString = "Host=localhost;Username=postgres;Password=yomo;Database=aidendb"
    let dbConnection = ensureDbConnection connectionString
    let childActorInstance = childActor dbConnection
    let childProps = Props.Create(fun () -> childActorInstance)
    let child = system.ActorOf(childProps, "child")
    let messageToSend = GetData
    let initialDelay: TimeSpan = TimeSpan.Zero
    let interval: TimeSpan = TimeSpan.FromSeconds(2.0)
    let sender = child
    let schedule = system.Scheduler.ScheduleTellRepeatedlyCancelable(initialDelay, interval, child, messageToSend, sender)
    let typedChild = child
    (schedule, typedChild)
