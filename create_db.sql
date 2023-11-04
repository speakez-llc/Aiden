-- Create the database
CREATE DATABASE aidendb;

-- Connect to the database and add extensions
\c aidendb;

-- Create extensions
CREATE EXTENSION timescaledb;
-- CREATE EXTENSION citus;
CREATE EXTENSION vector;
CREATE EXTENSION pg_trgm;
CREATE EXTENSION cube;
CREATE EXTENSION pgcrypto;
CREATE EXTENSION pg_prewarm;
CREATE EXTENSION "uuid-ossp";
CREATE EXTENSION amcheck;
CREATE EXTENSION pg_stat_statements;
CREATE EXTENSION tsm_system_rows;
CREATE EXTENSION tsm_system_time;
CREATE EXTENSION tcn;

-- Create the events table with the same columns
CREATE TABLE events (
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
