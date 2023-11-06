-- Create a new table that will eventually have more granular chunk size
CREATE TABLE events_hourly (LIKE events INCLUDING ALL);
-- Convert the new table into a hypertable specifying the chunk interval
SELECT create_hypertable('events_hourly', 'event_time', chunk_time_interval => interval '1 hour');
-- Copy data from the existing hypertable to the new hypertable
INSERT INTO events_hourly SELECT * FROM events;
-- Add retention policy (which actually yields 30 days of data as "today" counts as day 1)
SELECT add_retention_policy('events_hourly', INTERVAL '31 days');
-- Modify the job to run hourly in order to drop the last chunk(s) past 30 days in history
SELECT alter_job(1000, schedule_interval => INTERVAL '1 hour');
-- Add all of the relevant indices (indexes)
CREATE INDEX IF NOT EXISTS idx_btree_cst_id ON events_hourly USING btree (cst_id);
CREATE INDEX IF NOT EXISTS idx_gist_src_ip ON events_hourly USING gist (src_ip);
CREATE INDEX IF NOT EXISTS idx_btree_src_port ON events_hourly USING btree (src_port);
CREATE INDEX IF NOT EXISTS idx_gist_dst_ip ON events_hourly USING gist (dst_ip);
CREATE INDEX IF NOT EXISTS idx_btree_dst_port ON events_hourly USING btree (dst_port);
CREATE INDEX IF NOT EXISTS idx_btree_cc ON events_hourly (cc);
CREATE INDEX IF NOT EXISTS idx_trgm_events_vpn ON events_hourly USING gin (vpn gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_trgm_events_tor ON events_hourly USING gin (tor gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_trgm_events_proxy ON events_hourly USING gin (proxy gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_btree_mal ON events_hourly (malware);

-- Drop the original table which isn't needed any more
DROP TABLE events;



