namespace MyBlockchain.Core;

public class Blockchain
{
    // Fixed on purpose: every node must build the SAME genesis block (same content => same hash),
    // otherwise their chains could never be compatible.
    private static readonly DateTime GenesisTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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

        var genesis = new Block(0, GenesisTimestamp, Array.Empty<Transaction>(), "0");
        genesis.Mine(Difficulty);
        _chain.Add(genesis);
    }

    // Validates the transaction and puts it in the mempool (not in the chain yet).
    public bool TryAddTransaction(Transaction tx)
    {
        if (tx.IsSystem) return false;   // coins are only created by mining rewards
        if (string.IsNullOrWhiteSpace(tx.From) || string.IsNullOrWhiteSpace(tx.To)) return false;
        if (tx.From == tx.To) return false;
        if (tx.Amount <= 0) return false;

        if (!tx.HasValidSignature()) return false;   // is the sender really the owner?
        if (IsKnown(tx.Id)) return false;            // replay protection (also stops gossip loops)

        if (GetAvailableBalance(tx.From) < tx.Amount) return false;

        _pending.Add(tx);
        return true;
    }

    // Mines a block with the pending transactions. The miner earns the block reward.
    // Even with an empty mempool a block can be mined (it only contains the reward).
    public Block MinePendingTransactions(string minerAddress)
    {
        var coinbase = new Transaction(Transaction.SystemAddress, minerAddress, BlockReward);

        var block = new Block(Last.Index + 1, _pending.Prepend(coinbase), Last.Hash);
        block.Mine(Difficulty);

        _chain.Add(block);
        _pending.Clear();
        return block;
    }

    // Consensus: the longest VALID chain wins (with a fixed difficulty, length == work).
    // The candidate comes from an untrusted peer, so it goes through the full validation.
    public bool TryReplaceChain(IReadOnlyList<Block> candidate)
    {
        if (candidate.Count <= _chain.Count) return false;       // only strictly longer chains win
        if (candidate[0].Hash != _chain[0].Hash) return false;   // another network (different genesis)
        if (!IsValidChain(candidate)) return false;

        // Transactions from the abandoned blocks (and the current mempool) must not be lost:
        // whatever the new chain did not confirm gets another chance in the mempool.
        var confirmedIds = candidate.SelectMany(b => b.Transactions).Select(t => t.Id).ToHashSet();
        var toRecheck = _chain
            .SelectMany(b => b.Transactions)
            .Where(t => !t.IsSystem)
            .Concat(_pending)
            .Where(t => !confirmedIds.Contains(t.Id))
            .ToList();

        _chain.Clear();
        _chain.AddRange(candidate);
        _pending.Clear();

        foreach (var tx in toRecheck)
            TryAddTransaction(tx); // re-validated against the NEW chain (balances may have changed)

        return true;
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

    public bool IsValid() => IsValidChain(_chain);

    // Replays a whole chain from the genesis, exactly like a node joining the network would.
    private bool IsValidChain(IReadOnlyList<Block> chain)
    {
        if (chain.Count == 0) return false;

        var balances = new Dictionary<string, decimal>();
        var seenIds = new HashSet<string>();

        for (int i = 0; i < chain.Count; i++)
        {
            var block = chain[i];

            if (block.Index != i) return false;                                    // wrong position
            if (block.Hash != block.ComputeHash()) return false;                   // content was altered
            if (!block.MeetsDifficulty(Difficulty)) return false;                  // no valid proof of work
            if (i > 0 && block.PreviousHash != chain[i - 1].Hash) return false;    // link was broken
            if (!HasValidCoinbase(block)) return false;                            // minted wrong amount

            foreach (var tx in block.Transactions)
            {
                if (tx.Amount <= 0) return false;                  // negative amounts would steal funds
                if (!tx.HasValidSignature()) return false;         // forged or altered
                if (!seenIds.Add(tx.Id)) return false;             // same transaction twice

                if (!tx.IsSystem)
                {
                    var funds = balances.GetValueOrDefault(tx.From);
                    if (funds < tx.Amount) return false;           // spending coins that don't exist
                    balances[tx.From] = funds - tx.Amount;
                }

                balances[tx.To] = balances.GetValueOrDefault(tx.To) + tx.Amount;
            }
        }
        return true;
    }
}