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

-- Покрывающий индекс: равенство по клиенту, диапазон по дате, сумма в INCLUDE.
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
   WITH Days (Dt) AS
    (
    -- Шаг указан явно: при start > stop он по умолчанию -1, и обратный  интервал вернул бы строки вместо пустого результата.
    -- ISNULL — страховка от NULL в параметрах: -1 даёт пустой набор.
    SELECT DATEADD(day, g.value, @Sd)
    FROM GENERATE_SERIES(0, ISNULL(DATEDIFF(day, @Sd, @Ed), -1), 1) AS g
    ),
    Daily (Dt, Total) AS
    (
    -- Один диапазонный поиск по индексу на весь период,
    -- а не поиск на каждый день интервала.
    SELECT CAST(p.Dt AS date),
    SUM(p.Amount)
    FROM client.Payments AS p
    WHERE p.ClientId = @ClientId
    AND p.Dt >= @Sd
    AND p.Dt <  DATEADD(day, 1, @Ed)   -- полуинтервал: Ed включён целиком
    GROUP BY CAST(p.Dt AS date)
    )
SELECT Days.Dt,
       Amount = ISNULL(Daily.Total, 0)   -- день без платежей -> 0
FROM Days
         LEFT JOIN Daily ON Daily.Dt = Days.Dt;
GO
