using MyBlockchain.Core;

namespace MyBlockchain.Tests;

internal static class TestChains
{
    public const string Miner = "miner";

    // Chain where `owner` mined the first block and holds the 100 coin reward.
    public static Blockchain Funded(Wallet owner, int difficulty = 1)
    {
        var bc = new Blockchain(difficulty, blockReward: 100);
        bc.MinePendingTransactions(owner.Address);
        return bc;
    }
}