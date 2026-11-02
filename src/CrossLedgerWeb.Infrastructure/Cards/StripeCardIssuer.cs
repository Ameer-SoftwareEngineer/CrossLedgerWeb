using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Cards;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Issuing;

namespace CrossLedgerWeb.Infrastructure.Cards;

/// <summary>Issues a real Stripe Issuing Cardholder + Card in whatever mode Stripe:SecretKey
/// belongs to (test, almost certainly, given this project's own dev-grade posture) -
/// written against Stripe.net's documented API shape but NOT verified against a live
/// Stripe sandbox (no test credentials were available while building this). Only active
/// when CardIssuing:UseStripe is true; CardsController and IssueCardCommandHandler are
/// otherwise unchanged by its existence.
///
/// A freshly created Card's own Create response never includes the PAN/CVV (specification:
/// Stripe's Issuing Elements docs) - a second Retrieve call with expand=[number,cvc] gets
/// them, which Stripe permits unrestricted in test mode (the live-mode equivalent needs
/// Stripe's allow_retrieve_physical_card_details access, requested separately - out of
/// scope here, since this project never runs in live mode).
///
/// The Stripe card itself is left at its default "inactive" authorization status -
/// CrossLedger's own Card.Status (PendingApproval -&gt; Active, specification 9's Admin
/// Console gate) stays the only thing that decides whether a card can be used. Flipping
/// the Stripe-side status to match is a later slice's work, once the real-time
/// authorization webhook exists to actually act on it.</summary>
public sealed class StripeCardIssuer : ICardIssuer
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly string _secretKey;

    // Reads the same "Stripe:SecretKey" section CrossLedgerWeb.Providers' StripeOptions
    // binds too - a raw IConfiguration read rather than a shared options type, since
    // Infrastructure and Providers are kept as parallel siblings everywhere else in this
    // codebase (see WebhookNotifier's identical reasoning for HMAC signing), only
    // composed together at the Api layer.
    public StripeCardIssuer(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        var secretKey = configuration["Stripe:SecretKey"];
        _secretKey = string.IsNullOrWhiteSpace(secretKey)
            ? throw new InvalidOperationException("Missing 'Stripe:SecretKey' setting - required when CardIssuing:UseStripe is true.")
            : secretKey;
    }

    public async Task<IssuedCardDetails> IssueAsync(CardIssuanceRequest request, CancellationToken cancellationToken)
    {
        var owner = await _userManager.FindByIdAsync(request.OwnerId.Value.ToString())
            ?? throw new CardIssuanceFailedException(
                $"Cannot issue a Stripe card for owner {request.OwnerId} - no such user account exists.",
                new InvalidOperationException());

        try
        {
            var client = new StripeClient(_secretKey);

            var cardholder = await client.V1.Issuing.Cardholders.CreateAsync(
                new CardholderCreateOptions
                {
                    Type = "individual",
                    Name = request.CardholderName,
                    Email = owner.Email,
                    Billing = new CardholderBillingOptions
                    {
                        Address = new AddressOptions
                        {
                            Line1 = $"{request.BillingAddress.Number} {request.BillingAddress.Street}".Trim(),
                            City = request.BillingAddress.City,
                            State = request.BillingAddress.State,
                            PostalCode = request.BillingAddress.Postcode,
                            Country = ToIsoCountryCode(request.BillingAddress.Country),
                        },
                    },
                },
                cancellationToken: cancellationToken);

            var card = await client.V1.Issuing.Cards.CreateAsync(
                new Stripe.Issuing.CardCreateOptions
                {
                    Cardholder = cardholder.Id,
                    Currency = request.Currency.Code.ToLowerInvariant(),
                    Type = request.CardType == CardType.Physical ? "physical" : "virtual",
                },
                cancellationToken: cancellationToken);

            var revealed = await client.V1.Issuing.Cards.GetAsync(
                card.Id,
                new Stripe.Issuing.CardGetOptions { Expand = ["number", "cvc"] },
                cancellationToken: cancellationToken);

            return new IssuedCardDetails(revealed.Number, revealed.Cvc, (int)card.ExpMonth, (int)card.ExpYear, cardholder.Id, card.Id);
        }
        catch (StripeException ex)
        {
            throw new CardIssuanceFailedException($"Stripe rejected the card issuance request: {ex.Message}", ex);
        }
    }

    // CardAddress.Country stores a full country name (CrossLedgerFrontend's registration
    // form has no ISO-3166 package either, for the same reason) - Stripe requires a
    // 2-letter code, so this maps every entry the frontend's Countries.All list can send.
    private static string ToIsoCountryCode(string countryName) =>
        CountryCodesByName.TryGetValue(countryName, out var code)
            ? code
            : throw new CardIssuanceFailedException($"No ISO country code mapping for '{countryName}'.", new ArgumentOutOfRangeException(nameof(countryName)));

    private static readonly Dictionary<string, string> CountryCodesByName = new()
    {
        ["Afghanistan"] = "AF", ["Albania"] = "AL", ["Algeria"] = "DZ", ["Andorra"] = "AD", ["Angola"] = "AO",
        ["Argentina"] = "AR", ["Armenia"] = "AM", ["Australia"] = "AU", ["Austria"] = "AT", ["Azerbaijan"] = "AZ",
        ["Bahamas"] = "BS", ["Bahrain"] = "BH", ["Bangladesh"] = "BD", ["Barbados"] = "BB", ["Belarus"] = "BY",
        ["Belgium"] = "BE", ["Belize"] = "BZ", ["Benin"] = "BJ", ["Bhutan"] = "BT", ["Bolivia"] = "BO",
        ["Bosnia and Herzegovina"] = "BA", ["Botswana"] = "BW", ["Brazil"] = "BR", ["Brunei"] = "BN", ["Bulgaria"] = "BG",
        ["Burkina Faso"] = "BF", ["Burundi"] = "BI", ["Cambodia"] = "KH", ["Cameroon"] = "CM", ["Canada"] = "CA",
        ["Cape Verde"] = "CV", ["Central African Republic"] = "CF", ["Chad"] = "TD", ["Chile"] = "CL", ["China"] = "CN",
        ["Colombia"] = "CO", ["Comoros"] = "KM", ["Costa Rica"] = "CR", ["Croatia"] = "HR", ["Cuba"] = "CU",
        ["Cyprus"] = "CY", ["Czechia"] = "CZ", ["Denmark"] = "DK", ["Djibouti"] = "DJ", ["Dominica"] = "DM",
        ["Dominican Republic"] = "DO", ["Ecuador"] = "EC", ["Egypt"] = "EG", ["El Salvador"] = "SV", ["Equatorial Guinea"] = "GQ",
        ["Eritrea"] = "ER", ["Estonia"] = "EE", ["Eswatini"] = "SZ", ["Ethiopia"] = "ET", ["Fiji"] = "FJ",
        ["Finland"] = "FI", ["France"] = "FR", ["Gabon"] = "GA", ["Gambia"] = "GM", ["Georgia"] = "GE",
        ["Germany"] = "DE", ["Ghana"] = "GH", ["Greece"] = "GR", ["Grenada"] = "GD", ["Guatemala"] = "GT",
        ["Guinea"] = "GN", ["Guinea-Bissau"] = "GW", ["Guyana"] = "GY", ["Haiti"] = "HT", ["Honduras"] = "HN",
        ["Hungary"] = "HU", ["Iceland"] = "IS", ["India"] = "IN", ["Indonesia"] = "ID", ["Iran"] = "IR",
        ["Iraq"] = "IQ", ["Ireland"] = "IE", ["Israel"] = "IL", ["Italy"] = "IT", ["Jamaica"] = "JM",
        ["Japan"] = "JP", ["Jordan"] = "JO", ["Kazakhstan"] = "KZ", ["Kenya"] = "KE", ["Kiribati"] = "KI",
        ["Kuwait"] = "KW", ["Kyrgyzstan"] = "KG", ["Laos"] = "LA", ["Latvia"] = "LV", ["Lebanon"] = "LB",
        ["Lesotho"] = "LS", ["Liberia"] = "LR", ["Libya"] = "LY", ["Liechtenstein"] = "LI", ["Lithuania"] = "LT",
        ["Luxembourg"] = "LU", ["Madagascar"] = "MG", ["Malawi"] = "MW", ["Malaysia"] = "MY", ["Maldives"] = "MV",
        ["Mali"] = "ML", ["Malta"] = "MT", ["Marshall Islands"] = "MH", ["Mauritania"] = "MR", ["Mauritius"] = "MU",
        ["Mexico"] = "MX", ["Micronesia"] = "FM", ["Moldova"] = "MD", ["Monaco"] = "MC", ["Mongolia"] = "MN",
        ["Montenegro"] = "ME", ["Morocco"] = "MA", ["Mozambique"] = "MZ", ["Myanmar"] = "MM", ["Namibia"] = "NA",
        ["Nauru"] = "NR", ["Nepal"] = "NP", ["Netherlands"] = "NL", ["New Zealand"] = "NZ", ["Nicaragua"] = "NI",
        ["Niger"] = "NE", ["Nigeria"] = "NG", ["North Korea"] = "KP", ["North Macedonia"] = "MK", ["Norway"] = "NO",
        ["Oman"] = "OM", ["Pakistan"] = "PK", ["Palau"] = "PW", ["Palestine"] = "PS", ["Panama"] = "PA",
        ["Papua New Guinea"] = "PG", ["Paraguay"] = "PY", ["Peru"] = "PE", ["Philippines"] = "PH", ["Poland"] = "PL",
        ["Portugal"] = "PT", ["Qatar"] = "QA", ["Romania"] = "RO", ["Russia"] = "RU", ["Rwanda"] = "RW",
        ["Saint Kitts and Nevis"] = "KN", ["Saint Lucia"] = "LC", ["Saint Vincent and the Grenadines"] = "VC", ["Samoa"] = "WS", ["San Marino"] = "SM",
        ["Sao Tome and Principe"] = "ST", ["Saudi Arabia"] = "SA", ["Senegal"] = "SN", ["Serbia"] = "RS", ["Seychelles"] = "SC",
        ["Sierra Leone"] = "SL", ["Singapore"] = "SG", ["Slovakia"] = "SK", ["Slovenia"] = "SI", ["Solomon Islands"] = "SB",
        ["Somalia"] = "SO", ["South Africa"] = "ZA", ["South Korea"] = "KR", ["South Sudan"] = "SS", ["Spain"] = "ES",
        ["Sri Lanka"] = "LK", ["Sudan"] = "SD", ["Suriname"] = "SR", ["Sweden"] = "SE", ["Switzerland"] = "CH",
        ["Syria"] = "SY", ["Taiwan"] = "TW", ["Tajikistan"] = "TJ", ["Tanzania"] = "TZ", ["Thailand"] = "TH",
        ["Timor-Leste"] = "TL", ["Togo"] = "TG", ["Tonga"] = "TO", ["Trinidad and Tobago"] = "TT", ["Tunisia"] = "TN",
        ["Turkey"] = "TR", ["Turkmenistan"] = "TM", ["Tuvalu"] = "TV", ["Uganda"] = "UG", ["Ukraine"] = "UA",
        ["United Arab Emirates"] = "AE", ["United Kingdom"] = "GB", ["United States"] = "US", ["Uruguay"] = "UY", ["Uzbekistan"] = "UZ",
        ["Vanuatu"] = "VU", ["Vatican City"] = "VA", ["Venezuela"] = "VE", ["Vietnam"] = "VN", ["Yemen"] = "YE",
        ["Zambia"] = "ZM", ["Zimbabwe"] = "ZW",
    };
}
