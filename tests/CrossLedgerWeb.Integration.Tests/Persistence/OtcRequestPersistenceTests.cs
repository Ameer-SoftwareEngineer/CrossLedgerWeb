using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.Fx;
using CrossLedgerWeb.Domain.Otc;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Infrastructure.Persistence;
using CrossLedgerWeb.Infrastructure.Repositories;
using CrossLedgerWeb.Integration.Tests.TestSupport;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CrossLedgerWeb.Integration.Tests.Persistence;

// Same SQLite-in-memory rationale as WalletPersistenceTests/QuotePersistenceTests: it
// goes through the same relational query pipeline SQL Server does, so it actually
// exercises OtcRequestConfiguration's converters (OtcRequestIdConverter, CurrencyConverter,
// the Status enum-as-string mapping) and the nullable TransferId column - none of which
// CreateOtcRequestCommandHandlerTests/AcceptOtcRequestCommandHandlerTests touch, since
// those mock IOtcRequestRepository entirely.
public sealed class OtcRequestPersistenceTests : IDisposable
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly Currency Eur = Currency.From("EUR");

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<CrossLedgerWebDbContext> _options;

    public OtcRequestPersistenceTests()
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

    private static OtcRequest NewOfferedRequest(UserId requestedBy, DateTimeOffset requestedAt) => new(
        OtcRequestId.New(), requestedBy, WalletId.New(), QuoteId.New(),
        Usd, Eur, 15_000m, 0.8687m, requestedAt, requestedAt.AddMinutes(5));

    [Fact]
    public async Task An_offered_request_round_trips_with_no_transfer_id()
    {
        var now = DateTimeOffset.UtcNow;
        var request = NewOfferedRequest(UserId.New(), now);

        await using (var writeContext = new CrossLedgerWebDbContext(_options, TestDataProtection.Provider))
        {
            writeContext.OtcRequests.Add(request);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new CrossLedgerWebDbContext(_options, TestDataProtection.Provider);
        var reloaded = await new OtcRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);

        reloaded.Should().NotBeNull();
        reloaded!.Status.Should().Be(OtcRequestStatus.Offered);
        reloaded.TransferId.Should().BeNull();
        reloaded.FromCurrency.Should().Be(Usd);
        reloaded.ToCurrency.Should().Be(Eur);
        reloaded.RequestedAmount.Should().Be(15_000m);
        reloaded.DeskRate.Should().Be(0.8687m);
        reloaded.OfferExpiresAt.Should().Be(request.OfferExpiresAt);

        // The rehydrated request must behave identically to a freshly-created one.
        reloaded.IsExpired(now).Should().BeFalse();
    }

    [Fact]
    public async Task An_accepted_requests_transfer_id_round_trips_and_a_reloaded_request_still_rejects_a_second_accept()
    {
        var now = DateTimeOffset.UtcNow;
        var request = NewOfferedRequest(UserId.New(), now);
        var transferId = Guid.NewGuid();
        request.Accept(transferId);

        await using (var writeContext = new CrossLedgerWebDbContext(_options, TestDataProtection.Provider))
        {
            writeContext.OtcRequests.Add(request);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new CrossLedgerWebDbContext(_options, TestDataProtection.Provider);
        var reloaded = await new OtcRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);

        reloaded.Should().NotBeNull();
        reloaded!.Status.Should().Be(OtcRequestStatus.Accepted);
        reloaded.TransferId.Should().Be(transferId);

        // The entity's own invariant must still hold after EF's private rehydration ctor -
        // not just after the public ctor, which AcceptOtcRequestCommandHandler relies on to
        // stop a replayed accept from double-posting once the original request reloads.
        var act = () => reloaded.Accept(Guid.NewGuid());
        act.Should().Throw<InvalidOtcRequestStateException>();
    }

    [Fact]
    public async Task ListByRequesterAsync_returns_only_that_requesters_own_rows()
    {
        var now = DateTimeOffset.UtcNow;
        var ownerA = UserId.New();
        var ownerB = UserId.New();
        var requestA = NewOfferedRequest(ownerA, now);
        var requestB = NewOfferedRequest(ownerB, now);

        await using (var writeContext = new CrossLedgerWebDbContext(_options, TestDataProtection.Provider))
        {
            writeContext.OtcRequests.AddRange(requestA, requestB);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new CrossLedgerWebDbContext(_options, TestDataProtection.Provider);
        var results = await new OtcRequestRepository(readContext).ListByRequesterAsync(ownerA, CancellationToken.None);

        results.Should().ContainSingle(r => r.Id == requestA.Id);
        results.Should().NotContain(r => r.Id == requestB.Id);
    }
}
