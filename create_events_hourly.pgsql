-- Create a new hypertable with a more granular chunk size
CREATE TABLE events_hourly (LIKE events INCLUDING ALL);

-- Convert the new table into a hypertable specifying the chunk interval
SELECT create_hypertable('events_hourly', 'event_time', chunk_time_interval => interval '1 hour');

-- Copy data from the existing hypertable to the new hypertable
INSERT INTO events_hourly SELECT * FROM events;

-- Set a retention policy to drop chunks older than 31 days
SELECT add_retention_policy('events_hourly', INTERVAL '31 days');

DROP TABLE events;

SELECT * FROM timescaledb_information.chunks WHERE hypertable_name = 'events_hourly';


