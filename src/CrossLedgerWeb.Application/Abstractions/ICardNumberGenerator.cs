namespace CrossLedgerWeb.Application.Abstractions;

/// <summary>Generates a dev-simulated but Luhn-valid card number and a CVV - no real
/// card network or issuing processor sits behind these, same as DevLogSmsSender simulates
/// an SMS gateway.</summary>
public interface ICardNumberGenerator
{
    (string Pan, string Cvv) Generate();
}
