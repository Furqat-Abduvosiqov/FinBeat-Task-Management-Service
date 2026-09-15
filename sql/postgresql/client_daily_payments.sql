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
    SELECT days.dt::date,
           COALESCE(SUM(payments.amount), 0)::numeric(19, 4)
    FROM generate_series(p_start_date, p_end_date, INTERVAL '1 day') AS days(dt)
    LEFT JOIN client.payments
           ON payments.client_id = p_client_id
          AND payments.dt >= days.dt
          AND payments.dt <  days.dt + INTERVAL '1 day'
    GROUP BY days.dt
    ORDER BY days.dt;
$$;
