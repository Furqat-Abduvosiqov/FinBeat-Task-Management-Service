-- Задание 2 - daily payment totals per client, zero-filled.
--
-- Returns one row per calendar day in [@Sd, @Ed] inclusive, carrying the sum of that client's
-- payments on that day, or 0 where there were none.
--
-- SQL Server, matching the types the assignment states (bigint / datetime2(0) / money). The
-- assignment names the table ClientPayments in its DDL and client.Payments in its examples; the
-- schema-qualified spelling is used here. The PostgreSQL rendering under ../postgresql is the one
-- this repository can execute, and its output was checked against both worked examples.

IF SCHEMA_ID('client') IS NULL
    EXEC ('CREATE SCHEMA client');
GO

IF OBJECT_ID('client.Payments') IS NULL
    CREATE TABLE client.Payments
    (
        Id       bigint IDENTITY(1, 1) PRIMARY KEY,
        ClientId bigint       NOT NULL,
        Dt       datetime2(0) NOT NULL,
        Amount   money        NOT NULL
    );
GO

-- The function's access path. Leading with ClientId lets one client's rows be found directly, and
-- Dt second lets the range be walked in order rather than filtered afterwards.
IF INDEXPROPERTY(OBJECT_ID('client.Payments'), 'IX_Payments_ClientId_Dt', 'IndexID') IS NULL
    CREATE INDEX IX_Payments_ClientId_Dt ON client.Payments (ClientId, Dt) INCLUDE (Amount);
GO

CREATE OR ALTER FUNCTION client.GetDailyPayments
(
    @ClientId bigint,
    @Sd       date,
    @Ed       date
)
RETURNS TABLE
AS
RETURN
    -- A tally built by cross joins, not a recursive CTE: an inline table function cannot carry
    -- OPTION (MAXRECURSION 0), so recursion would stop at 100 days while the assignment says an
    -- interval may span several years.
    --
    -- Five doublings give 2^32 rows, past the 3,652,059 days the date type can express at all, so
    -- no interval can outrun the tally. Four stopped at 65,536 - about 179 years - and a longer
    -- request came back short with no error at all, which is a worse answer than a slow one. TOP
    -- below caps what is generated, so the unused levels cost nothing.
    --
    -- SQL Server 2022 and later could use GENERATE_SERIES instead; a tally runs on any version.
    WITH N0 (n) AS (SELECT 1 UNION ALL SELECT 1),
         N1 (n) AS (SELECT 1 FROM N0 a CROSS JOIN N0 b),
         N2 (n) AS (SELECT 1 FROM N1 a CROSS JOIN N1 b),
         N3 (n) AS (SELECT 1 FROM N2 a CROSS JOIN N2 b),
         N4 (n) AS (SELECT 1 FROM N3 a CROSS JOIN N3 b),
         N5 (n) AS (SELECT 1 FROM N4 a CROSS JOIN N4 b),
         Days (Dt) AS
         (
             -- CASE, because TOP rejects a negative count and @Sd may be later than @Ed.
             SELECT TOP (CASE WHEN DATEDIFF(day, @Sd, @Ed) < 0 THEN 0 ELSE DATEDIFF(day, @Sd, @Ed) + 1 END)
                    DATEADD(day, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1, @Sd)
             FROM N5
         )
    SELECT Days.Dt,
           Amount = ISNULL(SUM(p.Amount), 0)
    FROM Days
    -- Half-open range on the raw column rather than CAST(p.Dt AS date) = Days.Dt: casting the
    -- column would leave the index above unusable and force a scan of the client's every payment.
    LEFT JOIN client.Payments AS p
           ON p.ClientId = @ClientId
          AND p.Dt >= CAST(Days.Dt AS datetime2(0))
          AND p.Dt <  DATEADD(day, 1, CAST(Days.Dt AS datetime2(0)))
    GROUP BY Days.Dt;
GO
