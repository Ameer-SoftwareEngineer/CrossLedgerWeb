using CrossLedgerWeb.Application.Abstractions;

namespace CrossLedgerWeb.Infrastructure.Cards;

/// <summary>Generates a dev-simulated card number and CVV - no real card network or
/// issuing processor involved. The BIN prefix below is not a real issuer range, same
/// spirit as the well-known "4242 4242 4242 4242" test numbers card processors publish
/// for sandbox use.</summary>
public sealed class CardNumberGenerator : ICardNumberGenerator
{
    private const string TestBinPrefix = "453275";
    private const int PanLength = 16;

    public (string Pan, string Cvv) Generate()
    {
        var random = Random.Shared;
        var digits = new char[PanLength];
        TestBinPrefix.CopyTo(0, digits, 0, TestBinPrefix.Length);

        for (var i = TestBinPrefix.Length; i < PanLength - 1; i++)
            digits[i] = (char)('0' + random.Next(10));

        digits[PanLength - 1] = LuhnCheckDigit(digits.AsSpan(0, PanLength - 1));

        var cvv = random.Next(0, 1000).ToString("D3");

        return (new string(digits), cvv);
    }

    /// <summary>Standard Luhn check digit: scanning the payload right-to-left, the digit
    /// immediately left of where the check digit will sit is doubled, then alternating -
    /// the check digit itself is chosen so the full number's digit sum is a multiple of
    /// ten.</summary>
    private static char LuhnCheckDigit(ReadOnlySpan<char> payload)
    {
        var sum = 0;
        for (var i = 0; i < payload.Length; i++)
        {
            var digit = payload[payload.Length - 1 - i] - '0';
            var distanceFromCheckDigit = i + 1;
            if (distanceFromCheckDigit % 2 == 1)
            {
                digit *= 2;
                if (digit > 9)
                    digit -= 9;
            }

            sum += digit;
        }

        var checkDigit = (10 - sum % 10) % 10;
        return (char)('0' + checkDigit);
    }
}
