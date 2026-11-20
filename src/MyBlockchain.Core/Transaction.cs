using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MyBlockchain.Core;

public sealed record Transaction(string From, string To, decimal Amount)
{
    public const string SystemAddress = "SYSTEM";

    // Unique per transaction and part of the signed payload (replay protection).
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string? Signature { get; init; }

    public bool IsSystem => From == SystemAddress;

    // Exactly what gets signed. Culture-invariant so it is identical on every machine.
    public string Payload =>
        $"{Id}|{From}>{To}:{Amount.ToString(CultureInfo.InvariantCulture)}";

    // Used in the block hash: the signature is part of the block's content.
    public string Serialize() => $"{Payload}#{Signature}";

    public bool HasValidSignature()
    {
        if (IsSystem) return true; // Only coinbase transactions are "system"; chain rules restrict where they may appear
        if (string.IsNullOrEmpty(Signature)) return false;

        try
        {
            using var key = ECDsa.Create();
            key.ImportSubjectPublicKeyInfo(Convert.FromHexString(From), out _);

            return key.VerifyData(
                Encoding.UTF8.GetBytes(Payload),
                Convert.FromHexString(Signature),
                HashAlgorithmName.SHA256);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException or ArgumentException)
        {
            return false; // From is not a valid public key, or the signature is malformed
        }
    }
}