using System.Security.Cryptography;
using System.Text;

namespace MyBlockchain.Core;

public sealed class Wallet
{
    private readonly ECDsa _privateKey;

    // The address is the public key (hex). Safe to share.
    public string Address { get; }

    public Wallet()
    {
        _privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        Address = Convert.ToHexString(_privateKey.ExportSubjectPublicKeyInfo());
    }

    public Transaction CreateTransaction(string to, decimal amount)
    {
        var tx = new Transaction(Address, to, amount);
        var signature = _privateKey.SignData(Encoding.UTF8.GetBytes(tx.Payload), HashAlgorithmName.SHA256);
        return tx with { Signature = Convert.ToHexString(signature) };
    }
}