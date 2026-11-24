-- Closes the TOCTOU race DecideCardAuthorizationCommandHandler's own doc comment names:
-- two concurrent authorization requests against the same card could both read the same
-- "available balance" (wallet balance minus open-hold sum) before either's hold landed,
-- and both get approved. Mirrors usp_PostTransfer's own locking pattern - UPDLOCK,
-- HOLDLOCK escalates a ranged read to serializable isolation, so a second concurrent
-- caller cannot compute its own balance/open-hold total until this transaction commits.
--
-- Differs from usp_PostTransfer in one important way: insufficient funds is not an error
-- here, it is a normal decline outcome that still needs exactly one audit row (every
-- authorization decision, approved or declined, gets a CardAuthorizations row - see that
-- entity's own doc comment). So this never THROWs on a decline, it just inserts the
-- declined row and returns.
--
-- The CardId lock is defense-in-depth: today's 1:1 card-to-wallet invariant means the
-- WalletId lock alone already fully serializes two requests against the same card, but
-- locking CardAuthorizations too keeps this correct even if that invariant ever changes
-- (e.g. multiple cards sharing one wallet).
--
-- Error contract: 50002 WALLET_NOT_FOUND (same code usp_PostTransfer uses) - unreachable
-- in practice, since the caller already resolved the card's wallet before calling this.
CREATE OR ALTER PROCEDURE dbo.usp_ReserveCardAuthorizationHold
    @CardAuthorizationId   UNIQUEIDENTIFIER,
    @CardId                UNIQUEIDENTIFIER,
    @WalletId              UNIQUEIDENTIFIER,
    @StripeAuthorizationId NVARCHAR(255),
    @MerchantName          NVARCHAR(100),
    @Amount                DECIMAL(19, 4),
    @CurrencyCode          NVARCHAR(3),
    @DecidedAt             DATETIMEOFFSET(7),
    @ExpiresAtIfApproved   DATETIMEOFFSET(7)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Wallets WHERE Id = @WalletId)
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50002, 'WALLET_NOT_FOUND', 1;
        END

        DECLARE @Balance DECIMAL(19, 4) = ISNULL((
            SELECT SUM(CASE Direction WHEN 'Credit' THEN Amount WHEN 'Debit' THEN -Amount END)
            FROM dbo.LedgerEntries WITH (UPDLOCK, HOLDLOCK)
            WHERE WalletId = @WalletId
        ), 0);

        DECLARE @OpenHoldTotal DECIMAL(19, 4) = ISNULL((
            SELECT SUM(Amount)
            FROM dbo.CardAuthorizations WITH (UPDLOCK, HOLDLOCK)
            WHERE CardId = @CardId AND HoldStatus = 'Open' AND CurrencyCode = @CurrencyCode
        ), 0);

        DECLARE @Approved BIT = CASE WHEN (@Balance - @OpenHoldTotal) >= @Amount THEN 1 ELSE 0 END;
        DECLARE @DeclineReason NVARCHAR(50) = CASE WHEN @Approved = 0 THEN 'insufficient_funds' ELSE NULL END;
        DECLARE @HoldStatus NVARCHAR(20) = CASE WHEN @Approved = 1 THEN 'Open' ELSE 'NotApplicable' END;
        DECLARE @ExpiresAt DATETIMEOFFSET(7) = CASE WHEN @Approved = 1 THEN @ExpiresAtIfApproved ELSE NULL END;

        INSERT INTO dbo.CardAuthorizations
            (Id, CardId, StripeAuthorizationId, Amount, CurrencyCode, MerchantName, Approved, DeclineReason,
             HoldStatus, ExpiresAt, CapturedAmount, StripeTransactionId, TransferId, DecidedAt, ResolvedAt)
        VALUES
            (@CardAuthorizationId, @CardId, @StripeAuthorizationId, @Amount, @CurrencyCode, @MerchantName, @Approved, @DeclineReason,
             @HoldStatus, @ExpiresAt, NULL, NULL, NULL, @DecidedAt, NULL);

        COMMIT TRANSACTION;

        SELECT @Approved AS Approved, @DeclineReason AS DeclineReason;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
