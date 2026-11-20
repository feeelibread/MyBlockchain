namespace MyBlockchain.Core;

public class Blockchain
{
    private readonly List<Block> _chain = new();
    private readonly List<Transaction> _pending = new();

    public int Difficulty { get; }
    public decimal BlockReward { get; }
    public IReadOnlyList<Block> Chain => _chain;
    public IReadOnlyList<Transaction> PendingTransactions => _pending;
    public Block Last => _chain[^1];

    public Blockchain(int difficulty = 2, decimal blockReward = 50m)
    {
        Difficulty = difficulty;
        BlockReward = blockReward;

        var genesis = new Block(0, Array.Empty<Transaction>(), "0");
        genesis.Mine(Difficulty);
        _chain.Add(genesis);
    }

    // Validates the transaction and puts it in the mempool (not in the chain yet).
    public bool TryAddTransaction(Transaction tx)
    {
        if (tx.IsSystem) return false; // coin are only created by mining rewards
        if (string.IsNullOrWhiteSpace(tx.From) || string.IsNullOrWhiteSpace(tx.To)) return false;
        if (tx.From == tx.To) return false;
        if (tx.Amount <= 0) return false;

        if (!tx.HasValidSignature()) return false;   // is the sender really the owner?
        if (IsKnown(tx.Id)) return false;            // replay protection

        if (!tx.IsSystem && GetAvailableBalance(tx.From) < tx.Amount) return false;

        _pending.Add(tx);
        return true;
    }

    // Mines a block with the pending transactions. The miner earns the block reward.
    public Block MinePendingTransactions(string minerAddress)
    {
        var coinbase = new Transaction(Transaction.SystemAddress, minerAddress, BlockReward);

        var block = new Block(Last.Index + 1, _pending.Prepend(coinbase), Last.Hash);
        block.Mine(Difficulty);

        _chain.Add(block);
        _pending.Clear();
        return block;
    }

    // Confirmed balance: only transactions already inside mined blocks.
    public decimal GetBalance(string address)
    {
        decimal balance = 0;
        foreach (var tx in _chain.SelectMany(b => b.Transactions))
        {
            if (tx.To == address) balance += tx.Amount;
            if (tx.From == address) balance -= tx.Amount;
        }
        return balance;
    }

    // Confirmed balance minus what the address already committed in the mempool.
    private decimal GetAvailableBalance(string address) =>
        GetBalance(address) - _pending.Where(t => t.From == address).Sum(t => t.Amount);

    private bool IsKnown(string transactionId) =>
        _pending.Any(t => t.Id == transactionId) ||
        _chain.SelectMany(b => b.Transactions).Any(t => t.Id == transactionId);

    // Consensus rule: genesis is empty; every other block starts with exactly one
    // coinbase paying exactly BlockReward, and has no other system transaction.
    private bool HasValidCoinbase(Block block)
    {
        var txs = block.Transactions;

        if (block.Index == 0) return txs.Count == 0;

        if (txs.Count == 0 || !txs[0].IsSystem || txs[0].Amount != BlockReward) return false;
        return txs.Skip(1).All(t => !t.IsSystem);
    }


    // Replays the whole chain from the genesis, exactly lke a node joining a network would.
    public bool IsValid()
    {
        var balances = new Dictionary<string, decimal>();
        var seenIds = new HashSet<string>();

        for (int i = 0; i < _chain.Count; i++)
        {
            var block = _chain[i];
            if (block.Hash != block.ComputeHash()) return false; // content was altered
            if (!block.MeetsDifficulty(Difficulty)) return false; // no valid proof of work
            if (i > 0 && block.PreviousHash != _chain[i - 1].Hash) return false; // link was broken
            if (!HasValidCoinbase(block)) return false; // minted wrong amount

            foreach (var tx in block.Transactions)
            {
                if (tx.Amount <= 0) return false; // negative amounts would steal funds 
                if (!tx.HasValidSignature()) return false; // forged or altered
                if (!seenIds.Add(tx.Id)) return false; // same transaction twice

                if (!tx.IsSystem)
                {
                    var funds = balances.GetValueOrDefault(tx.From);
                    if (funds < tx.Amount) return false; // spending coins that doesn't exists
                    balances[tx.From] = funds - tx.Amount;
                }
                balances[tx.To] = balances.GetValueOrDefault(tx.To) + tx.Amount;
            }
        }
        return true;
    }
}