module Data

open System
open System.Data
open System.IO
open Akka.Actor
open Akkling
open Npgsql
open Microsoft.Extensions.Configuration

type DataReply = 
    | Data of (string * int64) list
    | NoData
    | Error of string

type ChildMessage =
    | GetData of AsyncReplyChannel<DataReply>
    | Terminate  
let mutable singletonDbConnection: NpgsqlConnection option = None

let configuration = 
    (ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("settings.json", optional = true, reloadOnChange = true)
        .Build())

let connectionString = configuration.GetConnectionString("ConnectionString")

let ensureDbConnection connectionString =
    match singletonDbConnection with
    | Some conn when conn.State <> System.Data.ConnectionState.Closed 
                   && conn.State <> System.Data.ConnectionState.Broken -> conn
    | _ ->
        // Close previous connection if any
        match singletonDbConnection with
        | Some oldConn -> 
            if oldConn.State <> System.Data.ConnectionState.Closed then
                oldConn.Close()
        | None -> ()
        
        // Create a new connection and assign it to the singleton
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
                // Execute the command and process results
                use cmd = new NpgsqlCommand(query, dbConnection)
                use reader = cmd.ExecuteReader()
                // Read results into a list of tuples (label, count)
                let results = 
                    [ while reader.Read() do
                        yield (
                            reader.GetString(reader.GetOrdinal("vpn_part")),
                            reader.GetInt64(reader.GetOrdinal("count"))
                        ) ]
                replyChannel.Reply (Data results)
                ()
            with
            | ex -> 
                replyChannel.Reply (Error ex.Message)
                ()
        | Terminate ->
            // Close the database connection if it's open
            if dbConnection.State = ConnectionState.Open then
                dbConnection.Close()
            // Stop the actor
            Stop
        return! loop ()
    }
    loop ()

let databaseParentActor(system: ActorSystem) : ICancelable * IActorRef<ChildMessage> =
    let connectionString = configuration.GetConnectionString("ConnectionString")
    let dbConnection = ensureDbConnection connectionString
    let childActorInstance = childActor dbConnection
    let childProps = Props.Create(fun () -> childActorInstance)
    let child = system.ActorOf(childProps, "vpn_child")
    let messageToSend = GetData

    let initialDelay: TimeSpan = TimeSpan.Zero
    let interval: TimeSpan = TimeSpan.FromSeconds(2.0)
    let sender: IActorRef = system.DeadLetters
    let schedule = system.Scheduler.ScheduleTellRepeatedlyCancelable(initialDelay, interval, child :> ICanTell, messageToSend, sender)
    let typedChild = child :?> IActorRef<ChildMessage>

    (schedule, typedChild)
