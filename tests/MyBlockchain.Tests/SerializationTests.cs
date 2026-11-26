using System.Text.Json;
using MyBlockchain.Core;
using static MyBlockchain.Tests.TestChains;

namespace MyBlockchain.Tests;

// Blocks travel between nodes as JSON. Nothing may be lost (or trusted) on the way.
[TestClass]
public class SerializationTests
{
    // Same defaults as ASP.NET Core and HttpClient.ReadFromJsonAsync.
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    private static Blockchain BuildChain(Wallet felipe, Wallet ana)
    {
        var bc = Funded(felipe);
        bc.TryAddTransaction(felipe.CreateTransaction(ana.Address, 30.5m));
        bc.MinePendingTransactions(Miner);
        return bc;
    }

    [TestMethod]
    public void Chain_SurvivesJsonRoundTrip_AndIsAcceptedByAnotherNode()
    {
        var felipe = new Wallet();
        var ana = new Wallet();
        var bc = BuildChain(felipe, ana);

        var json = JsonSerializer.Serialize(bc.Chain, Options);
        var received = JsonSerializer.Deserialize<List<Block>>(json, Options)!;

        // A fresh node adopts it: hashes, proof of work and signatures all survived the trip.
        var other = new Blockchain(difficulty: 1, blockReward: 100);
        Assert.IsTrue(other.TryReplaceChain(received));
        Assert.AreEqual(bc.Last.Hash, other.Last.Hash);
        Assert.AreEqual(30.5m, other.GetBalance(ana.Address));
        Assert.IsTrue(other.IsValid());
    }

    [TestMethod]
    public void Chain_WithForgedHashInJson_IsRejected()
    {
        var bc = BuildChain(new Wallet(), new Wallet());

        // The sender lies about the hash of the last block (it appears once in the JSON).
        var json = JsonSerializer.Serialize(bc.Chain, Options)
            .Replace(bc.Last.Hash, new string('0', 64));
        var received = JsonSerializer.Deserialize<List<Block>>(json, Options)!;

        var other = new Blockchain(difficulty: 1, blockReward: 100);
        Assert.IsFalse(other.TryReplaceChain(received));
    }
}