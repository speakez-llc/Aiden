-- Create the database
CREATE DATABASE aidendb;

-- Connect to the database and add extensions
\c aidendb;

-- Create extensions
CREATE EXTENSION IF NOT EXISTS timescaledb;
CREATE EXTENSION IF NOT EXISTS btree_gist;
CREATE EXTENSION IF NOT EXISTS vector;
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS cube;
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS pg_prewarm;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS amcheck;
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;
CREATE EXTENSION IF NOT EXISTS tsm_system_rows;
CREATE EXTENSION IF NOT EXISTS tsm_system_time;
CREATE EXTENSION IF NOT EXISTS tcn;

-- Create the events table with the same columns
CREATE TABLE IF NOT EXISTS events (
    event_time TIMESTAMPTZ NOT NULL,
    event_id UUID DEFAULT uuid_generate_v4(),
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

SELECT create_hypertable('events', 'event_time')

