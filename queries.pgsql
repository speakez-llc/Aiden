-- returns count of table in hypertable - takes about 2.5 seconds with 6.2mm records
SELECT COUNT(*) from events_hourly;

-- get detailed history of jobs in database
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

-- shows job information for retention policy in table  
SELECT * FROM timescaledb_information.jobs WHERE hypertable_name = 'events_hourly';

-- shows info on run status of policy
SELECT * FROM timescaledb_information.job_stats WHERE hypertable_name = 'events_hourly';

-- gets oldest record from the corpus
SELECT * FROM events_hourly ORDER BY event_time ASC LIMIT 1;

-- returns list of chunks for hypertable
SELECT * FROM timescaledb_information.chunks WHERE hypertable_name = 'events_hourly';

-- returns number of days and hours in the currnet corpus
SELECT
  FLOOR(EXTRACT(EPOCH FROM age) / 86400) AS days, -- Calculates the total days
  FLOOR((EXTRACT(EPOCH FROM age) % 86400) / 3600) AS hours -- Calculates the remaining hours after the full days are accounted for
FROM (
  SELECT age(CURRENT_TIMESTAMP AT TIME ZONE 'UTC', MIN(event_time)) AS age
  FROM events_hourly
) AS subquery;

-- returns all fields including partial matches in column for last time interval
SELECT
    UNNEST(string_to_array(tor, ';')) AS tor_part,
    COUNT(*) AS count
FROM
    events_hourly
WHERE
  event_time >= now() AT TIME ZONE 'UTC' - INTERVAL '1 minute'
GROUP BY
    tor_part;

SELECT
    UNNEST(string_to_array(proxy, ';')) AS pxy_part,
    COUNT(*) AS count
FROM
    events_hourly
WHERE
  event_time >= now() AT TIME ZONE 'UTC' - INTERVAL '1 minute'
GROUP BY
    pxy_part;

SELECT
    UNNEST(string_to_array(vpn, ';')) AS vpn_part,
    COUNT(*) AS count
FROM
    events_hourly
WHERE
  event_time >= now() AT TIME ZONE 'UTC' - INTERVAL '1 minute'
GROUP BY
    vpn_part;

-- returns count of all values for CC for a given interval
SELECT
    cc AS cc_part,
    COUNT(*) AS count
FROM
    events_hourly
WHERE
  event_time >= now() AT TIME ZONE 'UTC' - INTERVAL '1 minute'
GROUP BY
    cc_part;

SELECT
    malware AS mal_part,
    COUNT(*) AS count
FROM
    events_hourly
WHERE
  event_time >= now() AT TIME ZONE 'UTC' - INTERVAL '1 minute'
GROUP BY
    mal_part;


-- Get count of non-blank values for each column
SELECT
  COUNT(*) FILTER (WHERE vpn != 'BLANK') AS vpn_count,
  COUNT(*) FILTER (WHERE tor != 'BLANK') AS tor_count,
  COUNT(*) FILTER (WHERE proxy != 'BLANK') AS proxy_count
FROM
  events_hourly
WHERE
  event_time >= now() - INTERVAL '1 day';

-- This gets the time stamp of the latest record in the corpus
SELECT event_time AT TIME ZONE 'America/New_York' AS event_time_local FROM events_hourly
ORDER BY event_time DESC
LIMIT 1;

-- This returns the number of seconds from now to the latest record in the DB
SELECT EXTRACT(EPOCH FROM now() AT TIME ZONE 'UTC') - EXTRACT(EPOCH FROM MAX(event_time)) AS seconds_past
FROM events_hourly;
