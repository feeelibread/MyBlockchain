using System.Globalization;

namespace MyBlockchain.Core;

public sealed record Transaction(string From, string To, decimal Amount)
{
    public const string SystemAddress = "SYSTEM";
    public bool IsSystem => From == SystemAddress;

    // Culture-invariant on purpose: pt-BR formats decimal numbers with a comma, which would
    // make the same block produce a different hash different on different machines.
    public string Serialize() => $"{From} > {To}:{Amount.ToString(CultureInfo.InvariantCulture)}";
}