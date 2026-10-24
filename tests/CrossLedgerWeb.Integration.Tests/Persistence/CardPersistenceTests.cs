using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.Fx;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Infrastructure.Persistence;
using CrossLedgerWeb.Infrastructure.Repositories;
using CrossLedgerWeb.Integration.Tests.TestSupport;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CrossLedgerWeb.Integration.Tests.Persistence;

// Same SQLite-in-memory rationale as WalletPersistenceTests/TwoFactorCredentialPersistenceTests:
// it goes through the same relational query pipeline SQL Server does, so it actually
// exercises CardConfiguration's Pan/Cvv encryption, not just a round-trip through mocks.
public sealed class CardPersistenceTests : IDisposable
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset IssuedAt = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly CardAddress RegisteredAddress = new("123 Main St", "4B", "CA", "90001", "Los Angeles", "US");
    private static readonly CardAddress ShippingAddress = new("456 Other Ave", "7C", "NY", "10001", "New York", "US");

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<CrossLedgerWebDbContext> _options;

    public CardPersistenceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<CrossLedgerWebDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new CrossLedgerWebDbContext(_options, TestDataProtection.Provider);
        context.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private static Card NewCard() => new(
        CardId.New(), UserId.New(), WalletId.New(), Usd, CardType.Virtual, "Test Customer", "4532750123456784", "123", 6, 2029, IssuedAt,
        SourceOfFunds.Salary, 5000m, 100000m, true, "Acme Inc", "Developer", RegisteredAddress, ShippingAddress);

    [Fact]
    public async Task A_cards_pan_and_cvv_round_trip_decrypted_through_the_same_protector()
    {
        var card = NewCard();

        await using (var writeContext = new CrossLedgerWebDbContext(_options, TestDataProtection.Provider))
        {
            writeContext.Cards.Add(card);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new CrossLedgerWebDbContext(_options, TestDataProtection.Provider);
        var reloaded = await new CardRepository(readContext).GetByIdAsync(card.Id, CancellationToken.None);

        reloaded.Should().NotBeNull();
        reloaded!.Pan.Should().Be("4532750123456784");
        reloaded.Cvv.Should().Be("123");
        reloaded.Last4.Should().Be("6784");
        reloaded.Status.Should().Be(CardStatus.Active);
        reloaded.RegisteredAddress.Should().Be(RegisteredAddress);
        reloaded.ShippingAddress.Should().Be(ShippingAddress);
    }

    [Fact]
    public async Task The_pan_and_cvv_are_not_stored_in_plaintext_in_the_underlying_columns()
    {
        var card = NewCard();

        await using (var writeContext = new CrossLedgerWebDbContext(_options, TestDataProtection.Provider))
        {
            writeContext.Cards.Add(card);
            await writeContext.SaveChangesAsync();
        }

        await using var checkContext = new CrossLedgerWebDbContext(_options, TestDataProtection.Provider);
        var row = await checkContext.Database
            .SqlQueryRaw<RawCardRow>("SELECT EncryptedPan, EncryptedCvv FROM Cards LIMIT 1")
            .SingleAsync();

        row.EncryptedPan.Should().NotBe("4532750123456784");
        row.EncryptedCvv.Should().NotBe("123");
    }

    [Fact]
    public async Task A_frozen_cards_status_round_trips()
    {
        var card = NewCard();
        card.Freeze();
        card.SetSpendingLimit(250m);

        await using (var writeContext = new CrossLedgerWebDbContext(_options, TestDataProtection.Provider))
        {
            writeContext.Cards.Add(card);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new CrossLedgerWebDbContext(_options, TestDataProtection.Provider);
        var reloaded = await new CardRepository(readContext).GetByIdAsync(card.Id, CancellationToken.None);

        reloaded!.Status.Should().Be(CardStatus.Frozen);
        reloaded.SpendingLimit.Should().Be(250m);

        // The entity's own invariant must still hold after EF's private rehydration ctor.
        var act = () => reloaded.Freeze();
        act.Should().Throw<CrossLedgerWeb.Domain.Exceptions.InvalidCardStateException>();
    }

    private sealed class RawCardRow
    {
        public string EncryptedPan { get; set; } = string.Empty;
        public string EncryptedCvv { get; set; } = string.Empty;
    }
}
