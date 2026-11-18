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
            var felipe = new Wallet();
            var alice = new Wallet();
            var bc = Funded(felipe);
            bc.TryAddTransaction(felipe.CreateTransaction(alice.Address, 30m));
            bc.MinePendingTransactions();

            Assert.IsTrue(bc.IsValid());
        }

        [TestMethod]
        public void IsValid_ReturnsFalse_WhenTransactionIsTampered()
        {
            var felipe = new Wallet();
            var alice = new Wallet();
            var bc = Funded(felipe);
            bc.TryAddTransaction(felipe.CreateTransaction(alice.Address, 30m));
            bc.MinePendingTransactions();

            // Tamper with the chain
            bc.Chain[1].Transactions[0] = felipe.CreateTransaction(alice.Address, 300m);

            Assert.IsFalse(bc.IsValid());
        }

        [TestMethod]
        public void IsValid_ReturnsFalse_WhenTamperedBlockIsRehashed()
        {
            var felipe = new Wallet();
            var alice = new Wallet();
            var bc = Funded(felipe);
            bc.TryAddTransaction(felipe.CreateTransaction(alice.Address, 30m));
            bc.MinePendingTransactions();

            // Attacker edits block 1 AND recalculate its hash...
            bc.Chain[1].Transactions[0] = felipe.CreateTransaction(alice.Address, 300m);
            bc.Chain[1].Rehash();

            // but block 2 still points to the old hash of block 1.
            Assert.IsFalse(bc.IsValid());
        }

        [TestMethod]
        public void IsValid_ReturnsFalse_WhenBlockHasNoProofOfWork()
        {
            var bc = new Blockchain(difficulty: 3);
            bc.TryAddTransaction(new Transaction(Transaction.SystemAddress, "someone", 10));
            bc.MinePendingTransactions();

            var last = bc.Last;

            for (int i = 0; last.MeetsDifficulty(bc.Difficulty); i++)
            {
                last.Transactions[0] = new Transaction(Transaction.SystemAddress, "someone", 1000 + i);
                last.Rehash();
            }

            Assert.IsFalse(bc.IsValid());
        }

        [TestMethod]
        public void IsValid_ReturnsFalse_WhenSignedTransactionIsTamperedAndBlockIsRemined()
        {
            var felipe = new Wallet();
            var john = new Wallet();
            var bc = Funded(felipe);
            bc.TryAddTransaction(felipe.CreateTransaction(john.Address, 40m));
            bc.MinePendingTransactions();

            // Attacker changes the amount AND re-mines the (last) block:
            // hash, proof of work and link are all valid again...
            var last = bc.Last;
            last.Transactions[0] = last.Transactions[0] with { Amount = 90 };
            last.Mine(bc.Difficulty);

            // ... but the signature no longer matches the transaction
            Assert.IsFalse(bc.IsValid());
        }

    }
}