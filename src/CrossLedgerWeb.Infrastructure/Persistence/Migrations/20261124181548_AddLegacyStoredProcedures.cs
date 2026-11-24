using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrossLedgerWeb.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Recovers 8 stored procedures that have existed as live objects in the dev LocalDB
    /// since September 2026 but were never captured anywhere in source control - no .sql
    /// file, no database project, no prior migration created them. A fresh database (CI, a
    /// new machine, a cloud deploy) would be missing all of them. Source of truth now lives
    /// in db/procedures/*.sql (specification section 8) and here; CREATE OR ALTER makes
    /// this safe to run against both a database that already has these objects (today's
    /// dev LocalDB) and one that doesn't (everywhere else).
    /// </summary>
    public partial class AddLegacyStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_PostTransfer
    @TransferId                  UNIQUEIDENTIFIER,
    @SourceWalletId              UNIQUEIDENTIFIER,
    @FxSettlementSourceWalletId  UNIQUEIDENTIFIER,
    @FxSettlementTargetWalletId  UNIQUEIDENTIFIER,
    @TargetWalletId              UNIQUEIDENTIFIER,
    @SourceAmount                DECIMAL(19, 4),
    @SourceCurrencyCode          NVARCHAR(3),
    @TargetAmount                DECIMAL(19, 4),
    @TargetCurrencyCode          NVARCHAR(3),
    @PostedAt                    DATETIMEOFFSET(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @PostedAt IS NULL
        SET @PostedAt = SYSUTCDATETIME();

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @OrderedWalletIds TABLE (Seq INT IDENTITY(1, 1) PRIMARY KEY, WalletId UNIQUEIDENTIFIER NOT NULL);
        INSERT INTO @OrderedWalletIds (WalletId)
        SELECT DISTINCT v.WalletId
        FROM (VALUES (@SourceWalletId), (@FxSettlementSourceWalletId), (@FxSettlementTargetWalletId), (@TargetWalletId)) AS v(WalletId)
        ORDER BY v.WalletId;

        DECLARE @Wallets TABLE
        (
            WalletId  UNIQUEIDENTIFIER PRIMARY KEY,
            Kind      NVARCHAR(20)     NOT NULL,
            Currency  NVARCHAR(3)      NOT NULL,
            Balance   DECIMAL(19, 4)   NOT NULL
        );

        DECLARE @Seq INT = 1, @WalletCount INT = (SELECT COUNT(*) FROM @OrderedWalletIds);
        DECLARE @CurrentWalletId UNIQUEIDENTIFIER, @CurrentKind NVARCHAR(20), @CurrentCurrency NVARCHAR(3);

        WHILE @Seq <= @WalletCount
        BEGIN
            SELECT @CurrentWalletId = WalletId FROM @OrderedWalletIds WHERE Seq = @Seq;

            SELECT @CurrentKind = Kind, @CurrentCurrency = Currency
            FROM dbo.Wallets
            WHERE Id = @CurrentWalletId;

            IF @CurrentKind IS NULL
            BEGIN
                ROLLBACK TRANSACTION;
                THROW 50002, 'WALLET_NOT_FOUND', 1;
            END

            INSERT INTO @Wallets (WalletId, Kind, Currency, Balance)
            SELECT
                @CurrentWalletId,
                @CurrentKind,
                @CurrentCurrency,
                ISNULL((
                    SELECT SUM(CASE le.Direction WHEN 'Credit' THEN le.Amount WHEN 'Debit' THEN -le.Amount END)
                    FROM dbo.LedgerEntries AS le WITH (UPDLOCK, HOLDLOCK)
                    WHERE le.WalletId = @CurrentWalletId
                ), 0);

            SET @Seq += 1;
        END

        IF EXISTS (SELECT 1 FROM @Wallets WHERE WalletId IN (@SourceWalletId, @FxSettlementSourceWalletId) AND Currency <> @SourceCurrencyCode)
           OR EXISTS (SELECT 1 FROM @Wallets WHERE WalletId IN (@FxSettlementTargetWalletId, @TargetWalletId) AND Currency <> @TargetCurrencyCode)
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50003, 'CURRENCY_MISMATCH', 1;
        END

        IF EXISTS (
            SELECT 1 FROM @Wallets
            WHERE WalletId = @SourceWalletId AND Kind = 'Customer' AND Balance < @SourceAmount
        )
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50001, 'INSUFFICIENT_FUNDS', 1;
        END

        INSERT INTO dbo.LedgerEntries (Id, TransferId, WalletId, Direction, PostedAt, Amount, CurrencyCode)
        VALUES
            (NEWID(), @TransferId, @SourceWalletId,             'Debit',  @PostedAt, @SourceAmount, @SourceCurrencyCode),
            (NEWID(), @TransferId, @FxSettlementSourceWalletId, 'Credit', @PostedAt, @SourceAmount, @SourceCurrencyCode),
            (NEWID(), @TransferId, @FxSettlementTargetWalletId, 'Debit',  @PostedAt, @TargetAmount, @TargetCurrencyCode),
            (NEWID(), @TransferId, @TargetWalletId,             'Credit', @PostedAt, @TargetAmount, @TargetCurrencyCode);

        COMMIT TRANSACTION;

        SELECT @TransferId AS TransferId, 'COMPLETED' AS Status, @PostedAt AS PostedAt;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_GetWalletBalance
    @WalletId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Currency NVARCHAR(3) = (SELECT Currency FROM dbo.Wallets WHERE Id = @WalletId);

    IF @Currency IS NULL
        THROW 50002, 'WALLET_NOT_FOUND', 1;

    SELECT
        @WalletId AS WalletId,
        @Currency AS Currency,
        ISNULL(SUM(CASE Direction WHEN 'Credit' THEN Amount WHEN 'Debit' THEN -Amount END), 0) AS Balance
    FROM dbo.LedgerEntries
    WHERE WalletId = @WalletId;
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_GetAccountStatement
    @WalletId  UNIQUEIDENTIFIER,
    @FromDate  DATETIMEOFFSET(7),
    @ToDate    DATETIMEOFFSET(7)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @OpeningBalance DECIMAL(19, 4) = ISNULL((
        SELECT SUM(CASE Direction WHEN 'Credit' THEN Amount WHEN 'Debit' THEN -Amount END)
        FROM dbo.LedgerEntries
        WHERE WalletId = @WalletId AND PostedAt < @FromDate
    ), 0);

    SELECT @OpeningBalance AS OpeningBalance;

    SELECT
        Id, TransferId, Direction, Amount, CurrencyCode, PostedAt,
        @OpeningBalance + SUM(CASE Direction WHEN 'Credit' THEN Amount WHEN 'Debit' THEN -Amount END)
            OVER (ORDER BY PostedAt, Id ROWS UNBOUNDED PRECEDING) AS RunningBalance
    FROM dbo.LedgerEntries
    WHERE WalletId = @WalletId
      AND PostedAt >= @FromDate
      AND PostedAt <= @ToDate
    ORDER BY PostedAt, Id;
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_CheckTransferLimits
    @OwnerId          UNIQUEIDENTIFIER,
    @CurrencyCode     NVARCHAR(3),
    @ProposedAmount   DECIMAL(19, 4),
    @RollingLimit     DECIMAL(19, 4)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @WindowStart DATETIMEOFFSET(7) = DATEADD(HOUR, -24, SYSUTCDATETIME());

    DECLARE @RollingTotal DECIMAL(19, 4) = ISNULL((
        SELECT SUM(le.Amount)
        FROM dbo.LedgerEntries AS le
        INNER JOIN dbo.Wallets AS w ON w.Id = le.WalletId
        WHERE w.OwnerId = @OwnerId
          AND w.Kind = 'Customer'
          AND le.CurrencyCode = @CurrencyCode
          AND le.Direction = 'Debit'
          AND le.PostedAt >= @WindowStart
    ), 0);

    SELECT
        @RollingTotal AS RollingTotal,
        @RollingTotal + @ProposedAmount AS ProjectedTotal,
        @RollingLimit AS RollingLimit,
        CAST(CASE WHEN @RollingTotal + @ProposedAmount > @RollingLimit THEN 1 ELSE 0 END AS BIT) AS ExceedsLimit;
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_GetFxRateOHLC
    @FromCurrency  NVARCHAR(3),
    @ToCurrency    NVARCHAR(3),
    @FromDate      DATE,
    @ToDate        DATE
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH DailyRates AS (
        SELECT
            CAST(CapturedAt AS DATE) AS RateDate,
            MidMarketRate,
            ROW_NUMBER() OVER (PARTITION BY CAST(CapturedAt AS DATE) ORDER BY CapturedAt ASC)  AS OpenRank,
            ROW_NUMBER() OVER (PARTITION BY CAST(CapturedAt AS DATE) ORDER BY CapturedAt DESC) AS CloseRank
        FROM dbo.FxRateSnapshots
        WHERE FromCurrency = @FromCurrency
          AND ToCurrency = @ToCurrency
          AND CapturedAt >= CAST(@FromDate AS DATETIMEOFFSET)
          AND CapturedAt < CAST(DATEADD(DAY, 1, @ToDate) AS DATETIMEOFFSET)
    )
    SELECT
        RateDate,
        MAX(CASE WHEN OpenRank = 1 THEN MidMarketRate END)  AS [Open],
        MAX(MidMarketRate)                                    AS High,
        MIN(MidMarketRate)                                    AS Low,
        MAX(CASE WHEN CloseRank = 1 THEN MidMarketRate END) AS [Close]
    FROM DailyRates
    GROUP BY RateDate
    ORDER BY RateDate;
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_GetProviderStats
    @ProviderCode       NVARCHAR(20),
    @CurrencyCode       NVARCHAR(3),
    @DailyVolumeLimit   DECIMAL(19, 4)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RollingWindowStart DATETIMEOFFSET(7) = DATEADD(HOUR, -24, SYSUTCDATETIME());
    DECLARE @DayStart DATETIMEOFFSET(7) = CAST(CAST(SYSUTCDATETIME() AS DATE) AS DATETIMEOFFSET(7));

    DECLARE @OutcomeCount INT = (
        SELECT COUNT(*)
        FROM dbo.Payouts
        WHERE ProviderCode = @ProviderCode
          AND State IN ('Settled', 'ProviderFailed', 'PendingManual', 'Reversed')
          AND UpdatedAt >= @RollingWindowStart
    );

    DECLARE @SettledCount INT = (
        SELECT COUNT(*)
        FROM dbo.Payouts
        WHERE ProviderCode = @ProviderCode
          AND State = 'Settled'
          AND UpdatedAt >= @RollingWindowStart
    );

    DECLARE @TodayVolume DECIMAL(19, 4) = ISNULL((
        SELECT SUM(Amount)
        FROM dbo.Payouts
        WHERE ProviderCode = @ProviderCode
          AND CurrencyCode = @CurrencyCode
          AND UpdatedAt >= @DayStart
    ), 0);

    SELECT
        CASE WHEN @OutcomeCount = 0 THEN CAST(1 AS DECIMAL(19, 4))
             ELSE CAST(@SettledCount AS DECIMAL(19, 4)) / @OutcomeCount
        END AS RollingSuccessRate,
        CASE WHEN @DailyVolumeLimit <= 0 THEN CAST(0 AS DECIMAL(19, 4))
             ELSE CAST(
                CASE WHEN 1 - (@TodayVolume / @DailyVolumeLimit) < 0 THEN 0
                     WHEN 1 - (@TodayVolume / @DailyVolumeLimit) > 1 THEN 1
                     ELSE 1 - (@TodayVolume / @DailyVolumeLimit)
                END AS DECIMAL(19, 4))
        END AS RemainingDailyHeadroomRatio;
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_GetTransactionHistory
    @WalletId    UNIQUEIDENTIFIER,
    @PageNumber  INT = 1,
    @PageSize    INT = 20,
    @FromDate    DATETIMEOFFSET(7) = NULL,
    @ToDate      DATETIMEOFFSET(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageNumber < 1 SET @PageNumber = 1;
    IF @PageSize < 1 OR @PageSize > 200 SET @PageSize = 20;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    SELECT
        Id, TransferId, Direction, Amount, CurrencyCode, PostedAt,
        CASE Direction WHEN 'Credit' THEN Amount WHEN 'Debit' THEN -Amount END AS SignedAmount
    FROM dbo.LedgerEntries
    WHERE WalletId = @WalletId
      AND (@FromDate IS NULL OR PostedAt >= @FromDate)
      AND (@ToDate IS NULL OR PostedAt <= @ToDate)
    ORDER BY PostedAt DESC, Id DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    SELECT COUNT(*) AS TotalCount
    FROM dbo.LedgerEntries
    WHERE WalletId = @WalletId
      AND (@FromDate IS NULL OR PostedAt >= @FromDate)
      AND (@ToDate IS NULL OR PostedAt <= @ToDate);
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_ReconcileLedger
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Imbalances TABLE (CurrencyCode NVARCHAR(3) PRIMARY KEY, NetAmount DECIMAL(19, 4));

    INSERT INTO @Imbalances (CurrencyCode, NetAmount)
    SELECT
        CurrencyCode,
        SUM(CASE Direction WHEN 'Credit' THEN Amount WHEN 'Debit' THEN -Amount END) AS NetAmount
    FROM dbo.LedgerEntries
    GROUP BY CurrencyCode
    HAVING SUM(CASE Direction WHEN 'Credit' THEN Amount WHEN 'Debit' THEN -Amount END) <> 0;

    SELECT CurrencyCode, NetAmount FROM @Imbalances;

    IF EXISTS (SELECT 1 FROM @Imbalances)
        THROW 50004, 'LEDGER_IMBALANCE', 1;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_PostTransfer;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_GetWalletBalance;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_GetAccountStatement;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_CheckTransferLimits;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_GetFxRateOHLC;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_GetProviderStats;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_GetTransactionHistory;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_ReconcileLedger;");
        }
    }
}
