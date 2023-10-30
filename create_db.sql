-- Create the database
CREATE DATABASE aidendb;

-- Connect to the database and add extensions
\c aidendb;

-- Create extensions
CREATE EXTENSION timescaledb;
CREATE EXTENSION cstore_fdw;
CREATE EXTENSION vector;
CREATE EXTENSION pgcrypto;
CREATE EXTENSION "uuid-ossp";

-- Create the events table with the same columns
CREATE TABLE IF NOT EXISTS events (
    event_time TIMESTAMPTZ NOT NULL,
    event_id UUID DEFAULT CAST(REPLACE(CAST(uuid_generate_v4() AS TEXT), '-', '') AS UUID),
    cst_id UUID,
    src_ip INET,
    src_port INT,
    dst_ip INET,
    dst_port INT,
    cc TEXT,
    vpn TEXT,
    proxy TEXT,
    tor TEXT,
    malware TEXT,
    vector VECTOR
);

-- Create the hypertable for events based on the "event_time" column
SELECT create_hypertable('events', 'event_time');

-- Debugging: Check if the hypertable creation was successful
DO $$ 
BEGIN
   IF EXISTS (SELECT 1 FROM _timescaledb_catalog.hypertable WHERE schema_name = 'public' AND table_name = 'events') THEN
      RAISE NOTICE 'Hypertable "events" creation successful.';
   ELSE
      RAISE NOTICE 'Hypertable "events" creation failed.';
   END IF;
END $$;

DO $$ 
DECLARE
   sql_statement TEXT;
BEGIN
   sql_statement := 'CREATE FOREIGN TABLE events_cstore'
        || ' PARTITION OF events'
        || ' FOR VALUES FROM (''' || (CURRENT_DATE - INTERVAL '31 days') || ''') TO (''' || CURRENT_DATE || ''')'
        || ' SERVER cstore_server;';
   RAISE NOTICE 'SQL Statement: %', sql_statement;
   EXECUTE sql_statement;
EXCEPTION
   WHEN others THEN
      RAISE NOTICE 'Error: %', SQLERRM;
END $$;



-- Create the continuous aggregate policy
SELECT create_continuous_aggs_policy('events', start_offset => INTERVAL '1 day', end_offset => INTERVAL '1 day');

-- Create the vector index
CREATE INDEX events_vector_index ON events USING vector(vector);
