using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyBlockchain.Core;

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
        public void AddBlock_LinksToPreviousBlock()
        {
            var bc = new Blockchain();
            var genesis = bc.Last;
            var block = bc.AddBlock("A -> B: 10");

            Assert.AreEqual(1, block.Index);
            Assert.AreEqual(genesis.Hash, block.PreviousHash);
        }
    }
}