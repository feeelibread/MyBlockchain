using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyBlockchain.Core
{
    public class Blockchain
    {
        private readonly List<Block> _chain = new();
        public IReadOnlyList<Block> Chain => _chain;
        public Block Last => _chain[^1];
        public Blockchain() => _chain.Add(new Block(0, "Genesis", "0"));

        public Block AddBlock(string data)
        {
            var block = new Block(Last.Index + 1, data, Last.Hash);
            _chain.Add(block);
            return block;
        }

        public bool IsValid()
        {
            for (int i = 1; i < _chain.Count; i++)
            {
                var current = _chain[i];
                var previous = _chain[i - 1];

                if (current.Hash != current.ComputeHash())
                    return false; // content was altered

                if (current.PreviousHash != previous.Hash)
                    return false; // link was broken
            }
            return true;
        }
    }
}