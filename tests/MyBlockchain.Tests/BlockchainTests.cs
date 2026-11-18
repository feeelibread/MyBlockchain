using System.Diagnostics.CodeAnalysis;
using MyBlockchain.Core;
using static MyBlockchain.Tests.TestChains;

namespace MyBlockchain.Tests
{
    [TestClass]
    public class BlockchainTests
    {
        [TestMethod]
        public void NewChain_ConstainsOnlyGenesisBlock()
        {
            var bc = new Blockchain();
            Assert.HasCount(1, bc.Chain);
            Assert.AreEqual(0, bc.Last.Index);
            Assert.AreEqual("0", bc.Last.PreviousHash);
        }

        [TestMethod]
        public void MinePending_LinksBlocksToPrevioushBlockHash()
        {
            var bc = Funded(new Wallet());
            Assert.AreEqual(bc.Chain[0].Hash, bc.Last.PreviousHash);
        }

        [TestMethod]
        public void GetBalance_ReflectsMinedTransactions()
        {
            var felipe = new Wallet();
            var alice = new Wallet();
            var bc = Funded(felipe);
            bc.TryAddTransaction(felipe.CreateTransaction(alice.Address, 30));
            bc.MinePendingTransactions();

            Assert.AreEqual(70m, bc.GetBalance(felipe.Address));
            Assert.AreEqual(30m, bc.GetBalance(alice.Address));
        }

        [TestMethod]
        public void PendingTransactions_DoNotCountInConfirmedBalance()
        {
            var felipe = new Wallet();
            var alice = new Wallet();
            var bc = Funded(felipe);
            bc.TryAddTransaction(felipe.CreateTransaction(alice.Address, 30));

            Assert.HasCount(1, bc.PendingTransactions);
            Assert.AreEqual(100m, bc.GetBalance(felipe.Address));
            Assert.AreEqual(0m, bc.GetBalance(alice.Address));
        }

        [TestMethod]
        public void TryAddTransaction_RejectsInsufficientFunds()
        {
            var felipe = new Wallet();
            var alice = new Wallet();
            var victor = new Wallet();
            var bc = Funded(felipe);

            Assert.IsFalse(bc.TryAddTransaction(felipe.CreateTransaction(alice.Address, 101m)));
            Assert.IsFalse(bc.TryAddTransaction(alice.CreateTransaction(victor.Address, 1)));
            Assert.HasCount(0, bc.PendingTransactions);
        }

        [TestMethod]
        public void TryAddTransactoin_RejectsDoubleSpendInsideMempool()
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
        public void TryAddTransaction_RejectsInvalidTransactions()
        {
            var felipe = new Wallet();
            var alice = new Wallet();
            var bc = Funded(felipe);

            Assert.IsFalse(bc.TryAddTransaction(felipe.CreateTransaction(alice.Address, 0)));
            Assert.IsFalse(bc.TryAddTransaction(felipe.CreateTransaction(alice.Address, -1)));
            Assert.IsFalse(bc.TryAddTransaction(felipe.CreateTransaction(felipe.Address, 10))); // to self
            Assert.IsFalse(bc.TryAddTransaction(felipe.CreateTransaction("", 0))); // no recipient

            Assert.HasCount(0, bc.PendingTransactions);
        }

        [TestMethod]
        public void MinePending_ClearsMempool_AndReturnsNullWhenEmpty()
        {
            var felipe = new Wallet();
            var victor = new Wallet();
            var bc = Funded(felipe);
            bc.TryAddTransaction(felipe.CreateTransaction(victor.Address, 30m));

            Assert.IsNotNull(bc.MinePendingTransactions());
            Assert.HasCount(0, bc.PendingTransactions);
            Assert.IsNull(bc.MinePendingTransactions());
        }

    }
}