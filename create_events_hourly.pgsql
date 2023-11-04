-- Create a new hypertable with a more granular chunk size
CREATE TABLE events_hourly (LIKE events INCLUDING ALL);
-- Convert the new table into a hypertable specifying the chunk interval
SELECT create_hypertable('events_hourly', 'event_time', chunk_time_interval => interval '1 hour');
-- Copy data from the existing hypertable to the new hypertable
INSERT INTO events_hourly SELECT * FROM events;

SELECT add_retention_policy('events_hourly', INTERVAL '31 days');
SELECT alter_job(1000, schedule_interval => INTERVAL '1 hour');
CREATE INDEX idx_cst_id_btree ON events USING btree (cst_id);

DROP TABLE events;



