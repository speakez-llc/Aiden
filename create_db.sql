-- Create the database
CREATE DATABASE aidendb;

-- Connect to the database and add extensions
\c aidendb;

-- Create extensions
CREATE EXTENSION timescaledb;
CREATE EXTENSION cstore_fdw;
CREATE EXTENSION vector;
CREATE EXTENSION pg_trgm;
CREATE EXTENSION cube;
CREATE EXTENSION pgcrypto;
CREATE EXTENSION pg_prewarm;
CREATE EXTENSION "uuid-ossp";
CREATE EXTENSION amcheck;
CREATE EXTENSION pg_stat_statements;
CREATE EXTENSION plpgsql;

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


