using MyBlockchain.Core;
using static MyBlockchain.Tests.TestChains;

namespace MyBlockchain.Tests;

[TestClass]
public class SignatureTests
{
    [TestMethod]
    public void Wallets_HaveDifferentAddresses()
    {
        Assert.AreNotEqual(new Wallet().Address, new Wallet().Address);
    }

    [TestMethod]
    public void CreateTransaction_ProducesValidSignature()
    {
        var felipe = new Wallet();
        var tx = felipe.CreateTransaction(new Wallet().Address, 10);

        Assert.AreEqual(felipe.Address, tx.From);
        Assert.IsTrue(tx.HasValidSignature());
    }

    [TestMethod]
    public void TryAddTransaction_Rejects_UnsignedTransaction()
    {
        var felipe = new Wallet();
        var victor = new Wallet();
        var bc = Funded(felipe);

        var unsigned = new Transaction(felipe.Address, victor.Address, 10);

        Assert.IsFalse(bc.TryAddTransaction(unsigned));
    }

    [TestMethod]
    public void TryAddTransaction_Rejects_TamperedAmount()
    {
        var felipe = new Wallet();
        var victor = new Wallet();
        var bc = Funded(felipe);

        var signed = felipe.CreateTransaction(victor.Address, 10);
        var tampered = signed with { Amount = 100 };

        Assert.IsFalse(bc.TryAddTransaction(tampered));
    }

    [TestMethod]
    public void TryAddTransaction_Rejects_ForgedSender()
    {
        var felipe = new Wallet();
        var victor = new Wallet();
        var robert = new Wallet();
        var bc = Funded(felipe);

        // victor signs with HER key but claims the money comes from Felipe.
        var forged = victor.CreateTransaction(robert.Address, 50) with { From = felipe.Address };

        Assert.IsFalse(bc.TryAddTransaction(forged));
    }

    [TestMethod]
    public void TryAddTransaction_Rejects_ReplayedTransaction()
    {
        var felipe = new Wallet();
        var victor = new Wallet();
        var bc = Funded(felipe);

        var tx = felipe.CreateTransaction(victor.Address, 10);
        Assert.IsTrue(bc.TryAddTransaction(tx));
        bc.MinePendingTransactions(Miner);

        // Valid signature and enough balance, but this exact transaction already happened.
        Assert.IsFalse(bc.TryAddTransaction(tx));
        Assert.AreEqual(90m, bc.GetBalance(felipe.Address));
    }
}