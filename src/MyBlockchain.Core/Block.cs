using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace MyBlockchain.Core;

public class Block
{
    public int Index { get; }
    public DateTime Timestamp { get; }
    public string PreviousHash { get; }
    public long Nonce { get; private set; }
    public string Hash { get; private set; }

    // Mutable list on purpose: lets tests simulate tampering.
    public List<Transaction> Transactions { get; }

    // New block: the timestamp is "now".
    public Block(int index, IEnumerable<Transaction> transactions, string previousHash)
        : this(index, DateTime.UtcNow, transactions, previousHash)
    {
    }

    // Explicit timestamp: used by the deterministic genesis block.
    public Block(int index, DateTime timestamp, IEnumerable<Transaction> transactions, string previousHash)
    {
        Index = index;
        Timestamp = timestamp;
        Transactions = transactions.ToList(); // copy, so the caller can't change it later
        PreviousHash = previousHash;
        Hash = ComputeHash();
    }

    // Used by JSON deserialization to rebuild a block received from another node.
    // The parameter types must match the property types (List<Transaction>, not IEnumerable).
    // The hash is taken as sent and is NOT trusted: Blockchain.IsValid recomputes and checks it.
    [JsonConstructor]
    public Block(int index, DateTime timestamp, List<Transaction> transactions, string previousHash, long nonce, string hash)
        : this(index, timestamp, transactions, previousHash)
    {
        Nonce = nonce;
        Hash = hash;
    }

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