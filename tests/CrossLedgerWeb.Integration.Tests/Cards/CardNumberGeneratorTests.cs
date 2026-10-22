using CrossLedgerWeb.Infrastructure.Cards;
using FluentAssertions;

namespace CrossLedgerWeb.Integration.Tests.Cards;

public class CardNumberGeneratorTests
{
    private readonly CardNumberGenerator _generator = new();

    [Fact]
    public void Generate_produces_a_sixteen_digit_numeric_pan()
    {
        var (pan, _) = _generator.Generate();

        pan.Should().HaveLength(16);
        pan.Should().MatchRegex("^[0-9]{16}$");
    }

    [Fact]
    public void Generate_produces_a_luhn_valid_pan()
    {
        var (pan, _) = _generator.Generate();

        IsLuhnValid(pan).Should().BeTrue();
    }

    [Fact]
    public void Generate_produces_a_three_digit_numeric_cvv()
    {
        var (_, cvv) = _generator.Generate();

        cvv.Should().HaveLength(3);
        cvv.Should().MatchRegex("^[0-9]{3}$");
    }

    [Fact]
    public void Generate_never_produces_the_same_pan_twice_in_a_row()
    {
        var (first, _) = _generator.Generate();
        var (second, _) = _generator.Generate();

        first.Should().NotBe(second);
    }

    private static bool IsLuhnValid(string pan)
    {
        var sum = 0;
        for (var i = 0; i < pan.Length; i++)
        {
            var digit = pan[pan.Length - 1 - i] - '0';
            if (i % 2 == 1)
            {
                digit *= 2;
                if (digit > 9)
                    digit -= 9;
            }

            sum += digit;
        }

        return sum % 10 == 0;
    }
}
