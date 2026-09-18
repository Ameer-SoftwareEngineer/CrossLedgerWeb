namespace CrossLedgerWeb.Application.Admin;

public sealed record KycDocument(string FileName, string ContentType, byte[] Content);
