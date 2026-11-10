using MyBlockchain.Core;

namespace MyBlockchain.Tests
{
    // Each test is an attack (or the untouched case) on the blockchain. IsValid() must tell them apart.
    [TestClass]
    public class ConsensusTests
    {
        [TestMethod]
        public void IsValid_ReturnsTrue_ForUntouchedChain()
        {
            var bc = new Blockchain();
            bc.AddBlock("A -> B: 10");
            bc.AddBlock("B -> C: 3");

            Assert.IsTrue(bc.IsValid());
        }

        [TestMethod]
        public void IsValid_ReturnsFalse_WhenDataIsTampered()
        {
            var bc = new Blockchain();
            bc.AddBlock("A -> B: 10");
            bc.AddBlock("B -> C: 3");

            bc.Chain[1].Data = "A -> B: 1000";

            Assert.IsFalse(bc.IsValid());
        }

        [TestMethod]
        public void IsValid_ReturnsFalse_WhenTamperedBlockIsRehashed()
        {
            var bc = new Blockchain();
            bc.AddBlock("A -> B: 10");
            bc.AddBlock("B -> C: 3");

            // Attacker edits block 1 AND recalculates its hash...
            bc.Chain[1].Data = "A -> B: 1000";
            bc.Chain[1].Rehash();

            // ...but block 2 still points to the old hash.
            Assert.IsFalse(bc.IsValid());
        }

        [TestMethod]
        public void IsValid_ReturnsFalse_WhenBlockHasNoProofOfWork()
        {
            var bc = new Blockchain(difficulty: 3);
            bc.AddBlock("A -> B: 10");
            var last = bc.Last;

            //Has matches the content and link is intact (is the last block),
            // but it was NOT mined: Only the PoW check can catch this.
            for (int i = 0; last.MeetsDifficulty(3); i++)
            {
                last.Data = $"tampered-{i}";
                last.Rehash();
            }
            Assert.IsFalse(bc.IsValid());
        }

    }
}