SELECT COUNT(*) from events_hourly;

SELECT j.hypertable_name,
       j.job_id,
       config,
       schedule_interval,
       job_status,
       last_run_status,
       last_run_started_at,
       js.next_start,
       total_runs,
       total_successes,
       total_failures
  FROM timescaledb_information.jobs j
  JOIN timescaledb_information.job_stats js
    ON j.job_id = js.job_id
  WHERE j.proc_name = 'policy_retention';

  
SELECT * FROM timescaledb_information.jobs WHERE hypertable_name = 'events_hourly';

SELECT * FROM timescaledb_information.job_stats WHERE hypertable_name = 'events_hourly';

SELECT * FROM events_hourly ORDER BY event_time ASC LIMIT 1;

SELECT * FROM timescaledb_information.chunks WHERE hypertable_name = 'events_hourly';

SELECT
  FLOOR(EXTRACT(EPOCH FROM age) / 86400) AS days, -- Calculates the total days
  FLOOR((EXTRACT(EPOCH FROM age) % 86400) / 3600) AS hours -- Calculates the remaining hours after the full days are accounted for
FROM (
  SELECT age(CURRENT_TIMESTAMP AT TIME ZONE 'UTC', MIN(event_time)) AS age
  FROM events_hourly
) AS subquery;

