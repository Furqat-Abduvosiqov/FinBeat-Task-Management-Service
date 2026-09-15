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
    WITH N0 (n) AS (SELECT 1 UNION ALL SELECT 1),
         N1 (n) AS (SELECT 1 FROM N0 a CROSS JOIN N0 b),
         N2 (n) AS (SELECT 1 FROM N1 a CROSS JOIN N1 b),
         N3 (n) AS (SELECT 1 FROM N2 a CROSS JOIN N2 b),
         N4 (n) AS (SELECT 1 FROM N3 a CROSS JOIN N3 b),
         N5 (n) AS (SELECT 1 FROM N4 a CROSS JOIN N4 b),
         Days (Dt) AS
         (
             SELECT TOP (CASE WHEN DATEDIFF(day, @Sd, @Ed) < 0 THEN 0 ELSE DATEDIFF(day, @Sd, @Ed) + 1 END)
                    DATEADD(day, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1, @Sd)
             FROM N5
         )
    SELECT Days.Dt,
           Amount = ISNULL(SUM(p.Amount), 0)
    FROM Days
    LEFT JOIN client.Payments AS p
           ON p.ClientId = @ClientId
          AND p.Dt >= CAST(Days.Dt AS datetime2(0))
          AND p.Dt <  DATEADD(day, 1, CAST(Days.Dt AS datetime2(0)))
    GROUP BY Days.Dt;
GO
