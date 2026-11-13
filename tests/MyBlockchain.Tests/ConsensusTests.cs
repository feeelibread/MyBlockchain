using MyBlockchain.Core;
using static MyBlockchain.Tests.TestChains;

namespace MyBlockchain.Tests
{
    [TestClass]
    public class ConsensusTests
    {
        [TestMethod]
        public void IsValid_ReturnsTrue_ForUntouchableChain()
        {
            var bc = Funded();
            bc.TryAddTransaction(new Transaction("Felipe", "Alice", 30));
            bc.MinePendingTransactions();

            Assert.IsTrue(bc.IsValid());
        }

        [TestMethod]
        public void IsValid_ReturnsFalse_WhenTransactionIsTampered()
        {
            var bc = Funded();
            bc.TryAddTransaction(new Transaction("Felipe", "Alice", 30));
            bc.MinePendingTransactions();

            // Tamper with the chain
            bc.Chain[1].Transactions[0] = new Transaction("Felipe", "Alice", 3000);

            Assert.IsFalse(bc.IsValid());
        }

        [TestMethod]
        public void IsValid_ReturnsFalse_WhenTamperedBlockIsRehashed()
        {
            var bc = Funded();
            bc.TryAddTransaction(new Transaction("Felipe", "Alice", 30));
            bc.MinePendingTransactions();

            // Attacker edits block 1 AND recalculate its hash...
            bc.Chain[1].Transactions[0] = new Transaction("Felipe", "Alice", 3000);
            bc.Chain[1].Rehash();

            // but block 2 still points to the old hash of block 1.
            Assert.IsFalse(bc.IsValid());
        }

        [TestMethod]
        public void IsValid_ReturnsFalse_WhenBlockHasNoProofOfWork()
        {
            var bc = new Blockchain(difficulty: 3);
            bc.TryAddTransaction(new Transaction(Transaction.SystemAddress, "Felipe", 10));
            bc.MinePendingTransactions();

            var last = bc.Last;

            for (int i = 0; last.MeetsDifficulty(bc.Difficulty); i++)
            {
                last.Transactions[0] = new Transaction(Transaction.SystemAddress, "Felipe", 1000 + i);
                last.Rehash();
            }

            Assert.IsFalse(bc.IsValid());
        }

    }
}