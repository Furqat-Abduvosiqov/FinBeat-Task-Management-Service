-- Задание 2 - daily payment totals per client, zero-filled.
--
-- Returns one row per calendar day in [p_start_date, p_end_date] inclusive, carrying the sum of
-- that client's payments on that day, or 0 where there were none. Intervals may span years.
--
-- PostgreSQL. The assignment states the table in T-SQL types (bigint / datetime2(0) / money); the
-- equivalents here are bigint / timestamp / numeric(19,4). See ../sqlserver for a literal rendering.

CREATE SCHEMA IF NOT EXISTS client;

CREATE TABLE IF NOT EXISTS client.payments
(
    id        bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    client_id bigint         NOT NULL,
    dt        timestamp(0)   NOT NULL,
    amount    numeric(19, 4) NOT NULL
);

-- The function's access path. Leading with client_id lets one client's rows be found directly, and
-- dt second lets the range be walked in order rather than filtered afterwards.
CREATE INDEX IF NOT EXISTS ix_payments_client_id_dt ON client.payments (client_id, dt);

CREATE OR REPLACE FUNCTION client.get_daily_payments(
    p_client_id  bigint,
    p_start_date date,
    p_end_date   date)
RETURNS TABLE (dt date, amount numeric(19, 4))
LANGUAGE sql
STABLE
AS $$
    SELECT days.dt::date,
           COALESCE(SUM(payments.amount), 0)::numeric(19, 4)
    FROM generate_series(p_start_date, p_end_date, INTERVAL '1 day') AS days(dt)
    -- Half-open range on the raw column rather than date(payments.dt) = days.dt: casting the column
    -- would leave the index above unusable and force a scan of every one of the client's payments.
    LEFT JOIN client.payments
           ON payments.client_id = p_client_id
          AND payments.dt >= days.dt
          AND payments.dt <  days.dt + INTERVAL '1 day'
    GROUP BY days.dt
    ORDER BY days.dt;
$$;
