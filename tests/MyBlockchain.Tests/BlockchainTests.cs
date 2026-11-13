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
            var bc = Funded();
            Assert.AreEqual(bc.Chain[0].Hash, bc.Last.PreviousHash);
        }

        [TestMethod]
        public void GetBalance_ReflectsMinedTransactions()
        {
            var bc = Funded();
            bc.TryAddTransaction(new Transaction("Felipe", "Alice", 30));
            bc.MinePendingTransactions();

            Assert.AreEqual(70m, bc.GetBalance("Felipe"));
            Assert.AreEqual(30m, bc.GetBalance("Alice"));
        }

        [TestMethod]
        public void PendingTransactions_DoNotCountInConfirmedBalance()
        {
            var bc = Funded();
            bc.TryAddTransaction(new Transaction("Felipe", "Alice", 30));

            Assert.HasCount(1, bc.PendingTransactions);
            Assert.AreEqual(100m, bc.GetBalance("Felipe"));
            Assert.AreEqual(0m, bc.GetBalance("Alice"));
        }

        [TestMethod]
        public void TryAddTransaction_RejectsInsufficientFunds()
        {
            var bc = Funded();

            Assert.IsFalse(bc.TryAddTransaction(new Transaction("Felipe", "Alice", 101m)));
            Assert.IsFalse(bc.TryAddTransaction(new("Alice", "John", 1)));
            Assert.HasCount(0, bc.PendingTransactions);
        }

        [TestMethod]
        public void TryAddTransactoin_RejectsDoubleSpendInsideMempool()
        {
            var bc = Funded();

            Assert.IsTrue(bc.TryAddTransaction(new("Felipe", "Victor", 60m)));
            Assert.IsFalse(bc.TryAddTransaction(new("Felipe", "Robert", 60m)));
            Assert.IsTrue(bc.TryAddTransaction(new("Felipe", "Robert", 40m)));
        }

        [TestMethod]
        public void TryAddTransaction_RejectsInvalidTransactions()
        {
            var bc = Funded();

            Assert.IsFalse(bc.TryAddTransaction(new Transaction("", "Alice", 1)));
            Assert.IsFalse(bc.TryAddTransaction(new Transaction("Alice", "", 1)));
            Assert.IsFalse(bc.TryAddTransaction(new Transaction("Alice", "Alice", 1)));
            Assert.IsFalse(bc.TryAddTransaction(new Transaction("Alice", "Victor", 0)));
            Assert.IsFalse(bc.TryAddTransaction(new Transaction("Alice", "Roberto", -1)));

            Assert.HasCount(0, bc.PendingTransactions);
        }

        [TestMethod]
        public void MinePending_ClearsMempool_AndReturnsNullWhenEmpty()
        {
            var bc = Funded();
            bc.TryAddTransaction(new("Felipe", "Victor", 10));

            Assert.IsNotNull(bc.MinePendingTransactions());
            Assert.HasCount(0, bc.PendingTransactions);
            Assert.IsNull(bc.MinePendingTransactions());
        }

    }
}