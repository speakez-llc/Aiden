-- Create the database if it doesn't exist
CREATE DATABASE aidendb;

-- Connect to the database and add extensions if they don't exist
\c aidendb;

-- Create extensions
CREATE EXTENSION IF NOT EXISTS timescaledb;
CREATE EXTENSION IF NOT EXISTS cstore_fdw;
CREATE EXTENSION IF NOT EXISTS vector;

-- Create the events table if it doesn't exist
CREATE TABLE IF NOT EXISTS events (
    EventTime TIMESTAMPTZ NOT NULL,
    cst_id UUID,
    src_ip INET,
    src_port INT,
    dst_ip INET,
    dst_port INT,
    cc TEXT,
    vpn TEXT,
    proxy TEXT,
    tor TEXT,
    malware TEXT  -- adjusted from BOOLEAN to TEXT
);