using MyBlockchain.Core;
using static MyBlockchain.Tests.TestChains;

namespace MyBlockchain.Tests;

// Longest-chain consensus: what a node does when a peer shows a different chain.
[TestClass]
public class ChainReplacementTests
{
    private static Blockchain NewNode() => new(difficulty: 1, blockReward: 100);

    [TestMethod]
    public void Genesis_IsIdenticalOnEveryNode()
    {
        Assert.AreEqual(NewNode().Chain[0].Hash, NewNode().Chain[0].Hash);
    }

    [TestMethod]
    public void TryReplaceChain_Accepts_LongerValidChain()
    {
        var mine = NewNode();
        mine.MinePendingTransactions("a");

        var other = NewNode();
        other.MinePendingTransactions("b");
        other.MinePendingTransactions("b");

        Assert.IsTrue(mine.TryReplaceChain(other.Chain));
        Assert.HasCount(3, mine.Chain);
        Assert.AreEqual(other.Last.Hash, mine.Last.Hash);
        Assert.AreEqual(0m, mine.GetBalance("a"));     // our own block was abandoned
        Assert.AreEqual(200m, mine.GetBalance("b"));
    }

    [TestMethod]
    public void TryReplaceChain_Rejects_ShorterOrEqualChain()
    {
        var mine = NewNode();
        mine.MinePendingTransactions("a");
        mine.MinePendingTransactions("a");

        var sameLength = NewNode();
        sameLength.MinePendingTransactions("b");
        sameLength.MinePendingTransactions("b");

        Assert.IsFalse(mine.TryReplaceChain(sameLength.Chain)); // tie: keep ours
        Assert.IsFalse(mine.TryReplaceChain(NewNode().Chain));  // shorter
    }

    [TestMethod]
    public void TryReplaceChain_Rejects_LongerChainThatIsInvalid()
    {
        var mine = NewNode();
        var other = NewNode();
        other.MinePendingTransactions("b");
        other.MinePendingTransactions("b");

        other.Chain[1].Transactions[0] = other.Chain[1].Transactions[0] with { Amount = 1000 };

        Assert.IsFalse(mine.TryReplaceChain(other.Chain));
        Assert.HasCount(1, mine.Chain);
    }

    [TestMethod]
    public void TryReplaceChain_Rejects_ChainFromAnotherNetwork()
    {
        var mine = NewNode();
        var foreign = new Blockchain(difficulty: 2, blockReward: 100); // different genesis
        foreign.MinePendingTransactions("b");
        foreign.MinePendingTransactions("b");

        Assert.IsFalse(mine.TryReplaceChain(foreign.Chain));
    }

    [TestMethod]
    public void TryReplaceChain_ReturnsOrphanedTransactionsToTheMempool()
    {
        var felipe = new Wallet();
        var ana = new Wallet();

        // Shared history: [genesis, block mined by Felipe].
        var nodeA = Funded(felipe);
        var nodeB = NewNode();
        Assert.IsTrue(nodeB.TryReplaceChain(nodeA.Chain));

        // The network forks: A confirms a payment, B mines two blocks.
        nodeA.TryAddTransaction(felipe.CreateTransaction(ana.Address, 30));
        nodeA.MinePendingTransactions("minerA");
        nodeB.MinePendingTransactions("minerB");
        nodeB.MinePendingTransactions("minerB");

        Assert.IsTrue(nodeA.TryReplaceChain(nodeB.Chain)); // B is longer

        Assert.AreEqual(0m, nodeA.GetBalance(ana.Address));   // the payment was undone...
        Assert.HasCount(1, nodeA.PendingTransactions);  // ...and waits to be mined again
    }

    [TestMethod]
    public void TryReplaceChain_DropsPendingTransactionsAlreadyInTheNewChain()
    {
        var felipe = new Wallet();
        var ana = new Wallet();

        var nodeA = Funded(felipe);
        var nodeB = NewNode();
        nodeB.TryReplaceChain(nodeA.Chain);

        // Both nodes heard the same transaction (gossip), but only B mined it.
        var payment = felipe.CreateTransaction(ana.Address, 30);
        nodeA.TryAddTransaction(payment);
        nodeB.TryAddTransaction(payment);
        nodeB.MinePendingTransactions("minerB");
        nodeB.MinePendingTransactions("minerB");

        Assert.IsTrue(nodeA.TryReplaceChain(nodeB.Chain));

        Assert.HasCount(0, nodeA.PendingTransactions);
        Assert.AreEqual(30m, nodeA.GetBalance(ana.Address));
    }
}