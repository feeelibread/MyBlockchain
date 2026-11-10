using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyBlockchain.Core;

namespace MyBlockchain.Tests
{
    [TestClass]
    public class BlockTests
    {
        [TestMethod]
        public void ComputeHash_IsDeterministic()
        {
            var block = new Block(1, "data", "prev");
            Assert.AreEqual(block.ComputeHash(), block.ComputeHash());
        }

        [TestMethod]
        public void ComputeHash_ChangesWhenDataChanges()
        {
            var block = new Block(1, "data", "prev");
            var before = block.ComputeHash();
            block.Data = "new data";

            Assert.AreNotEqual(before, block.ComputeHash());
        }

        [TestMethod]
        public void Mine_ProduceHashWithRequiredLeadingZeros()
        {
            var block = new Block(1, "data", "prev");
            block.Mine(3);

            Assert.StartsWith("000", block.Hash);
            Assert.AreEqual(block.ComputeHash(), block.Hash);
        }

    }
}