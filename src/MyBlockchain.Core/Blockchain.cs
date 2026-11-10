namespace MyBlockchain.Core;

public class Blockchain
{
    private readonly List<Block> _chain = new();

    public int Difficulty { get; }
    public IReadOnlyList<Block> Chain => _chain;
    public Block Last => _chain[^1];

    public Blockchain(int difficulty = 2)
    {
        Difficulty = difficulty;

        var genesis = new Block(0, "Genesis", "0");
        genesis.Mine(Difficulty);
        _chain.Add(genesis);
    }

    public Block AddBlock(string data)
    {
        var block = new Block(Last.Index + 1, data, Last.Hash);
        block.Mine(Difficulty);
        _chain.Add(block);
        return block;
    }

    public bool IsValid()
    {
        for (int i = 0; i < _chain.Count; i++)
        {
            var current = _chain[i];

            if (current.Hash != current.ComputeHash()) return false;   // content was altered
            if (!current.MeetsDifficulty(Difficulty)) return false;    // no valid proof of work

            if (i > 0 && current.PreviousHash != _chain[i - 1].Hash)   // link was broken
                return false;
        }
        return true;
    }
}