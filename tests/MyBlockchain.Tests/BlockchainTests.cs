using MyBlockchain.Core;
using static MyBlockchain.Tests.TestChains;

namespace MyBlockchain.Tests;

[TestClass]
public class BlockchainTests
{
    // ---- Chain structure and mining ----

    [TestMethod]
    public void NewChain_ContainsOnlyGenesisBlock()
    {
        var bc = new Blockchain(1);

        Assert.HasCount(1, bc.Chain);
        Assert.AreEqual(0, bc.Last.Index);
        Assert.AreEqual("0", bc.Last.PreviousHash);
    }

    [TestMethod]
    public void MinePending_LinksBlockToPreviousBlockHash()
    {
        var bc = new Blockchain(1);
        var genesis = bc.Last;

        var block = bc.MinePendingTransactions(Miner);

        Assert.AreEqual(1, block.Index);
        Assert.AreEqual(genesis.Hash, block.PreviousHash);
    }

    [TestMethod]
    public void MinePending_MinesBlockAccordingToChainDifficulty()
    {
        var bc = new Blockchain(difficulty: 3);

        var block = bc.MinePendingTransactions(Miner);

        Assert.IsTrue(block.MeetsDifficulty(3));
        Assert.IsTrue(bc.Chain[0].MeetsDifficulty(3)); // genesis is mined too
    }

    [TestMethod]
    public void MinePending_AddsCoinbaseRewardAsFirstTransaction()
    {
        var bc = new Blockchain(1, blockReward: 100);

        var block = bc.MinePendingTransactions(Miner);

        var coinbase = block.Transactions[0];
        Assert.IsTrue(coinbase.IsSystem);
        Assert.AreEqual(Miner, coinbase.To);
        Assert.AreEqual(100m, coinbase.Amount);
        Assert.AreEqual(100m, bc.GetBalance(Miner));
    }

    [TestMethod]
    public void MinePending_ClearsMempool_AndEmptyBlockOnlyHasTheReward()
    {
        var felipe = new Wallet();
        var victor = new Wallet();
        var bc = Funded(felipe);
        bc.TryAddTransaction(felipe.CreateTransaction(victor.Address, 10));

        var withTx = bc.MinePendingTransactions(Miner);
        Assert.IsEmpty(bc.PendingTransactions);
        Assert.HasCount(2, withTx.Transactions); // coinbase + transfer

        var empty = bc.MinePendingTransactions(Miner);
        Assert.HasCount(1, empty.Transactions);  // coinbase only
    }

    // ---- Balances and mempool ----

    [TestMethod]
    public void GetBalance_ReflectsMinedTransactions()
    {
        var felipe = new Wallet();
        var victor = new Wallet();
        var bc = Funded(felipe);
        bc.TryAddTransaction(felipe.CreateTransaction(victor.Address, 30));
        bc.MinePendingTransactions(Miner);

        Assert.AreEqual(70m, bc.GetBalance(felipe.Address));
        Assert.AreEqual(30m, bc.GetBalance(victor.Address));
        Assert.AreEqual(100m, bc.GetBalance(Miner));
    }

    [TestMethod]
    public void PendingTransactions_DoNotCountInConfirmedBalance()
    {
        var felipe = new Wallet();
        var victor = new Wallet();
        var bc = Funded(felipe);

        bc.TryAddTransaction(felipe.CreateTransaction(victor.Address, 30));

        Assert.HasCount(1, bc.PendingTransactions);
        Assert.AreEqual(100m, bc.GetBalance(felipe.Address));
        Assert.AreEqual(0m, bc.GetBalance(victor.Address));
    }

    [TestMethod]
    public void TryAddTransaction_Rejects_InsufficientFunds()
    {
        var felipe = new Wallet();
        var victor = new Wallet();
        var robert = new Wallet();
        var bc = Funded(felipe);

        Assert.IsFalse(bc.TryAddTransaction(felipe.CreateTransaction(victor.Address, 101)));
        Assert.IsFalse(bc.TryAddTransaction(victor.CreateTransaction(robert.Address, 1))); // victor has nothing
        Assert.IsEmpty(bc.PendingTransactions);
    }

    [TestMethod]
    public void TryAddTransaction_Rejects_DoubleSpendInsideMempool()
    {
        var felipe = new Wallet();
        var victor = new Wallet();
        var robert = new Wallet();
        var bc = Funded(felipe);

        Assert.IsTrue(bc.TryAddTransaction(felipe.CreateTransaction(victor.Address, 80)));
        Assert.IsFalse(bc.TryAddTransaction(felipe.CreateTransaction(robert.Address, 80))); // only 20 left
        Assert.IsTrue(bc.TryAddTransaction(felipe.CreateTransaction(robert.Address, 20)));
    }

    [TestMethod]
    public void TryAddTransaction_Rejects_InvalidTransactions()
    {
        var felipe = new Wallet();
        var victor = new Wallet();
        var bc = Funded(felipe);

        Assert.IsFalse(bc.TryAddTransaction(felipe.CreateTransaction(victor.Address, 0)));
        Assert.IsFalse(bc.TryAddTransaction(felipe.CreateTransaction(victor.Address, -5)));
        Assert.IsFalse(bc.TryAddTransaction(felipe.CreateTransaction(felipe.Address, 10))); // to self
        Assert.IsFalse(bc.TryAddTransaction(felipe.CreateTransaction("", 10)));             // no recipient
    }

    [TestMethod]
    public void TryAddTransaction_Rejects_SystemTransactions()
    {
        var bc = new Blockchain(1);

        // Nobody can mint coins through the mempool.
        Assert.IsFalse(bc.TryAddTransaction(new Transaction(Transaction.SystemAddress, "thief", 1000)));
    }
}