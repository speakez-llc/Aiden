module Aiden.Services.Data

open System
open System.Data
open System.IO
open Akka.Actor
open Akkling
open Npgsql
open Microsoft.Extensions.Configuration

type ChildMessage =
    | FetchData
    | GetData of AsyncReplyChannel<string>
    | Terminate  

// Singleton for the database connection
let mutable singletonDbConnection: NpgsqlConnection option = None

// Load configuration from settings.json file
let configuration = 
    (ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("settings.json", optional = true, reloadOnChange = true)
        .Build())

// Read the connection string from the configuration
let connectionString = configuration.GetConnectionString("ConnectionString")

// Function to ensure a singleton database connection
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
        @"SELECT UNNEST(string_to_array(vpn, ';')) AS vpn_part, COUNT(*) AS count
          FROM events_hourly
          WHERE event_time >= now() AT TIME ZONE 'UTC' - INTERVAL '1 minute'
          GROUP BY vpn_part;"

    let rec loop () = actor {
        let! msg = mailbox.Receive()
        match msg with
        | FetchData ->
            // Execute the command and process results
            use cmd = new NpgsqlCommand(query, dbConnection)
            // (Add logic for executing command and processing results)
            ()
        | GetData replyChannel ->
            // Execute the command and process results
            use cmd = new NpgsqlCommand(query, dbConnection)
            // (Add logic for executing command and processing results)
            replyChannel.Reply "Some results"
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

let databaseParentActor (system: ActorSystem) =
    let connectionString = configuration.GetConnectionString("ConnectionString")
    let dbConnection = ensureDbConnection connectionString
    
    // Define the function to create a child actor instance
    let childActorInstance = childActor dbConnection

    // Use actorOf to spawn the child actor
    let child = system.ActorOf(Props.Create childActorInstance, "vpn_pie_child")

    // Define the message to send
    let messageToSend = FetchData

    // Define scheduling parameters
    let initialDelay: TimeSpan = TimeSpan.Zero
    let interval: TimeSpan = TimeSpan.FromSeconds(2.0)
    let sender: IActorRef = system.DeadLetters

    // Use the Akkling idiomatic way of scheduling messages
    let schedule = system.Scheduler.ScheduleTellRepeatedlyCancelable(initialDelay, interval, child, messageToSend, sender)
    
    // Keep a reference to the schedule if you need to cancel it later
    schedule




