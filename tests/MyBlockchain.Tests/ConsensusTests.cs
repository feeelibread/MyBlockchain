using MyBlockchain.Core;

namespace MyBlockchain.Tests
{
    // Each test is an attack (or the untouched case) on the blockchain. IsValid() must tell them apart.
    [TestClass]
    public class ConsensusTests
    {
        [TestMethod]
        public void UntouchedChain_IsValid()
        {
            var bc = new Blockchain();
            bc.AddBlock("A -> B: 10");
            bc.AddBlock("B -> C: 5");

            Assert.IsTrue(bc.IsValid());
        }

        [TestMethod]
        public void TamperedData_IsInvalid()
        {
            var bc = new Blockchain();
            bc.AddBlock("A -> B: 10");
            var block = bc.AddBlock("B -> C: 5");

            block.Data = "B -> C: 100"; // attacker changes the data

            Assert.IsFalse(bc.IsValid());
        }

        [TestMethod]
        public void RehashedData_IsInvalid()
        {
            var bc = new Blockchain();
            bc.AddBlock("A -> B: 10");
            bc.AddBlock("B -> C: 5");

            bc.Chain[1].Data = "B -> C: 100"; // attacker changes the data
            bc.Chain[1].Rehash(); // attacker recalculates the hash

            // The chain is still invalid because the previous block's hash doesn't match the new hash.
            Assert.IsFalse(bc.IsValid());
        }

        [TestMethod]
        public void RehashedData_IsValid()
        {
            var bc = new Blockchain();
            bc.AddBlock("A -> B: 10");
            bc.Chain[1].Data = "B -> C: 100"; // attacker changes the data
            bc.Chain[1].Rehash(); // attacker recalculates the hash

            bc.AddBlock("B -> C: 5");

            // The chain is valid because the new block's previous hash matches the recalculated hash of the tampered block.
            Assert.IsTrue(bc.IsValid());
        }

    }
}