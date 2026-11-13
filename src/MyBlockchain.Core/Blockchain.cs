namespace MyBlockchain.Core;

public class Blockchain
{
    private readonly List<Block> _chain = new();
    private readonly List<Transaction> _pending = new();

    public int Difficulty { get; }
    public IReadOnlyList<Block> Chain => _chain;
    public IReadOnlyList<Transaction> PendingTransactions => _pending;
    public Block Last => _chain[^1];

    public Blockchain(int difficulty = 2)
    {
        Difficulty = difficulty;

        var genesis = new Block(0, Array.Empty<Transaction>(), "0");
        genesis.Mine(Difficulty);
        _chain.Add(genesis);
    }

    //Validates the transaction and adds it to the mempool (not in the chain yet). Returns false if the transaction is invalid.
    public bool TryAddTransaction(Transaction tx)
    {
        if (String.IsNullOrWhiteSpace(tx.From) || String.IsNullOrWhiteSpace(tx.To)) return false;
        if (tx.From == tx.To) return false;
        if (tx.Amount <= 0) return false;

        if (!tx.IsSystem && GetAvailableBalance(tx.From) < tx.Amount) return false;

        _pending.Add(tx);
        return true;
    }

    // Packs all pending transactions into a new block, mines it, and adds it to the chain.
    public Block? MinePendingTransactions()
    {
        if (_pending.Count == 0) return null;

        var block = new Block(Last.Index + 1, _pending, Last.Hash);
        block.Mine(Difficulty);
        _chain.Add(block);
        _pending.Clear();
        return block;
    }

    //Confirmed Balance: only transactions already inside mined blocks.
    public decimal GetBalance(string address)
    {
        decimal balance = 0;
        foreach (var tx in _chain.SelectMany(b => b.Transactions))
        {
            if (tx.From == address) balance -= tx.Amount;
            if (tx.To == address) balance += tx.Amount;
        }
        return balance;
    }

    // Confirmed Balance minus what the address already committed in the mempool.
    private decimal GetAvailableBalance(string address) =>
    GetBalance(address) - _pending.Where(t => t.From == address).Sum(t => t.Amount);

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