using System.Security.Cryptography;
using System.Text;

namespace MyBlockchain.Core
{
    public class Block
    {
        public int Index { get; set; }
        public DateTime Timestamp { get; }
        public string PreviousHash { get; }
        public string Hash { get; private set; }

        // Public setter on purpose: lets tests simulate tampering.
        public string Data { get; set; }

        public Block(int index, string data, string previousHash)
        {
            Index = index;
            Data = data;
            PreviousHash = previousHash;
            Timestamp = DateTime.UtcNow;
            Hash = ComputeHash();
        }

        public string ComputeHash()
        {
            var raw = $"{Index}|{Timestamp}|{Data}|{PreviousHash}";
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes);
        }

        // Used by tests to simulate an attacker who recalculates a hash.
        public void Rehash() => Hash = ComputeHash();
    }
}