using System.Security.Cryptography;
using System.Text;

namespace MyBlockchain.Core;

public class Block
{
    public int Index { get; }
    public DateTime Timestamp { get; }
    public string PreviousHash { get; }
    public long Nonce { get; private set; }
    public string Hash { get; private set; }

    // Public list on purpose: lets tests simulate tampering.
    public List<Transaction> Transactions { get; set; } = new();

    public Block(int index, IEnumerable<Transaction> transaction, string previousHash)
    {
        Index = index;
        Timestamp = DateTime.UtcNow;
        Transactions = transaction.ToList(); // copy, so that the caller can't modify it later
        PreviousHash = previousHash;
        Hash = ComputeHash();
    }

    // The nonce is now part of the hashed content.
    public string ComputeHash()
    {
        var data = string.Join(";", Transactions.Select(t => t.Serialize()));
        var raw = $"{Index}|{Timestamp:O}|{data}|{PreviousHash}|{Nonce}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }

    // Proof of Work: try nonces until the hash starts with `difficulty` zeros.
    public void Mine(int difficulty)
    {
        var prefix = new string('0', difficulty);
        Nonce = 0;
        Hash = ComputeHash();

        while (!Hash.StartsWith(prefix, StringComparison.Ordinal))
        {
            Nonce++;
            Hash = ComputeHash();
        }
    }

    public bool MeetsDifficulty(int difficulty) =>
        Hash.StartsWith(new string('0', difficulty), StringComparison.Ordinal);

    // Used by tests to simulate an attacker who recalculates a hash without mining.
    public void Rehash() => Hash = ComputeHash();
}