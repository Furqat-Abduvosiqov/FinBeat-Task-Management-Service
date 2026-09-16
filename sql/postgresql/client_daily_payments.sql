CREATE SCHEMA IF NOT EXISTS client;

CREATE TABLE IF NOT EXISTS client.payments
(
    id        bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    client_id bigint         NOT NULL,
    dt        timestamp(0)   NOT NULL,
    amount    numeric(19, 4) NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_payments_client_id_dt
    ON client.payments (client_id, dt) INCLUDE (amount);

CREATE OR REPLACE FUNCTION client.get_daily_payments(
    p_client_id  bigint,
    p_start_date date,
    p_end_date   date)
RETURNS TABLE (dt date, amount numeric(19, 4))
LANGUAGE sql
STABLE
PARALLEL SAFE
AS $$
    WITH daily AS (
        SELECT payments.dt::date AS day,
               SUM(payments.amount) AS total
        FROM client.payments
        WHERE payments.client_id = p_client_id
          AND payments.dt >= p_start_date
          AND payments.dt <  p_end_date + 1
        GROUP BY 1)
    SELECT days.day::date,
           COALESCE(daily.total, 0)::numeric(19, 4)
    FROM generate_series(p_start_date::timestamp, p_end_date::timestamp, INTERVAL '1 day') AS days(day)
    LEFT JOIN daily ON daily.day = days.day::date
    ORDER BY days.day;
$$;
