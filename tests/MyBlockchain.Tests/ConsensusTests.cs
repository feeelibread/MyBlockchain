using MyBlockchain.Core;
using static MyBlockchain.Tests.TestChains;

namespace MyBlockchain.Tests;

// Each test is an attack. IsValid() must catch all of them.
[TestClass]
public class ConsensusTests
{
    [TestMethod]
    public void IsValid_ReturnsTrue_ForUntouchedChain()
    {
        var felipe = new Wallet();
        var victor = new Wallet();
        var bc = Funded(felipe);
        bc.TryAddTransaction(felipe.CreateTransaction(victor.Address, 30));
        bc.MinePendingTransactions(Miner);

        Assert.IsTrue(bc.IsValid());
    }

    [TestMethod]
    public void IsValid_ReturnsFalse_WhenTransactionIsTampered()
    {
        var felipe = new Wallet();
        var bc = Funded(felipe);

        bc.Chain[1].Transactions[0] = new Transaction(Transaction.SystemAddress, felipe.Address, 1_000_000);

        Assert.IsFalse(bc.IsValid());
    }

    [TestMethod]
    public void IsValid_ReturnsFalse_WhenTamperedBlockIsRehashed()
    {
        var felipe = new Wallet();
        var victor = new Wallet();
        var bc = Funded(felipe);
        bc.TryAddTransaction(felipe.CreateTransaction(victor.Address, 30));
        bc.MinePendingTransactions(Miner);

        // Attacker edits block 1 AND recalculates its hash...
        bc.Chain[1].Transactions[0] = new Transaction(Transaction.SystemAddress, felipe.Address, 1_000_000);
        bc.Chain[1].Rehash();

        // ...but block 2 still points to the old hash.
        Assert.IsFalse(bc.IsValid());
    }

    [TestMethod]
    public void IsValid_ReturnsFalse_WhenBlockHasNoProofOfWork()
    {
        var bc = new Blockchain(difficulty: 3);
        bc.MinePendingTransactions(Miner);
        var last = bc.Last;

        // Keeps the coinbase rules satisfied (still a valid reward, just to another address)
        // and the hash coherent, but the block is NOT mined: only the PoW check catches this.
        for (int i = 0; last.MeetsDifficulty(3); i++)
        {
            last.Transactions[0] = new Transaction(Transaction.SystemAddress, $"miner-{i}", bc.BlockReward);
            last.Rehash();
        }

        Assert.IsFalse(bc.IsValid());
    }

    [TestMethod]
    public void IsValid_ReturnsFalse_WhenSignedTransactionIsTamperedAndBlockIsRemined()
    {
        var felipe = new Wallet();
        var victor = new Wallet();
        var bc = Funded(felipe);
        bc.TryAddTransaction(felipe.CreateTransaction(victor.Address, 30));
        bc.MinePendingTransactions(Miner);

        // Attacker changes the amount AND re-mines the (last) block:
        // hash, proof of work and link are all valid again...
        var last = bc.Last;
        last.Transactions[1] = last.Transactions[1] with { Amount = 90 };
        last.Mine(bc.Difficulty);

        // ...but the signature no longer matches the transaction.
        Assert.IsFalse(bc.IsValid());
    }

    [TestMethod]
    public void IsValid_ReturnsFalse_WhenMinerPaysItselfMoreThanTheReward()
    {
        var bc = new Blockchain(1, blockReward: 100);
        bc.MinePendingTransactions(Miner);

        // Miner re-mines its own block with a bigger reward: PoW is valid, the rule is not.
        var last = bc.Last;
        last.Transactions[0] = last.Transactions[0] with { Amount = 1000 };
        last.Mine(bc.Difficulty);

        Assert.IsFalse(bc.IsValid());
    }

    [TestMethod]
    public void IsValid_ReturnsFalse_WhenBlockHasASecondCoinbase()
    {
        var bc = new Blockchain(1, blockReward: 100);
        bc.MinePendingTransactions(Miner);

        var last = bc.Last;
        last.Transactions.Add(new Transaction(Transaction.SystemAddress, "thief", 100));
        last.Mine(bc.Difficulty);

        Assert.IsFalse(bc.IsValid());
    }

    [TestMethod]
    public void IsValid_ReturnsFalse_WhenBlockContainsAnOverspend()
    {
        var felipe = new Wallet();
        var victor = new Wallet();
        var bob = new Wallet();
        var bc = Funded(felipe);

        // Malicious miner includes a correctly signed transaction from someone with no funds.
        var last = bc.Last;
        last.Transactions.Add(victor.CreateTransaction(bob.Address, 50));
        last.Mine(bc.Difficulty);

        Assert.IsFalse(bc.IsValid());
    }

    [TestMethod]
    public void IsValid_ReturnsFalse_WhenBlockContainsANegativeAmount()
    {
        var felipe = new Wallet();
        var victor = new Wallet();
        var bc = Funded(felipe);

        // victor signs "I send -50 to Felipe", which would pull coins out of his balance.
        var last = bc.Last;
        last.Transactions.Add(victor.CreateTransaction(felipe.Address, -50));
        last.Mine(bc.Difficulty);

        Assert.IsFalse(bc.IsValid());
    }
}