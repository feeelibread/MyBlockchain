using System.Net.Http.Json;
using Microsoft.Extensions.Http;
using MyBlockchain.Core;

namespace MyBlockchain.Node
{

    // The network layer. Wraps ONE Blockchain, serializes access to it (The Core is not thread-safe, and ASP.NET handles request concurrently) 
    // and talks to the peers.
    public class BlockchainNode
    {
        private static readonly TimeSpan PeerTimeout = TimeSpan.FromSeconds(5);

        private readonly object _lock = new();
        private readonly Blockchain _blockchain;
        private readonly HashSet<string> _peers = [];
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<BlockchainNode> _logger;

        // The node's own wallet: its address is the miner address, and it can send coins (a convenience for the demo; real wallets lives outside the node);
        public Wallet Wallet { get; }

        public BlockchainNode(Blockchain blockchain, Wallet wallet, IHttpClientFactory httpFactory, ILogger<BlockchainNode> logger)
        {
            _blockchain = blockchain;
            Wallet = wallet;
            _httpFactory = httpFactory;
            _logger = logger;
        }

        // ---------- reads (returns always copy, never the live collections) ---------- 
        public List<Block> GetChain()
        {
            lock (_lock) { return _blockchain.Chain.ToList(); }
        }

        public List<Transaction> GetPending()
        {
            lock (_lock) { return _blockchain.PendingTransactions.ToList(); }
        }

        public decimal GetBalance(string address)
        {
            lock (_lock) { return _blockchain.GetBalance(address); }
        }

        public List<string> Peers()
        {
            lock (_lock) { return _peers.ToList(); }
        }

        public object GetStatus()
        {
            lock (_lock)
            {
                return new
                {
                    length = _blockchain.Chain.Count,
                    difficulty = _blockchain.Difficulty,
                    blockReward = _blockchain.BlockReward,
                    pending = _blockchain.PendingTransactions.Count,
                    peers = _peers.Count,
                    valid = _blockchain.IsValid()
                };
            }
        }

        public List<string> GetPeers()
        {
            lock (_lock) { return _peers.ToList(); }
        }

        // ---------- peers ---------- 
        public bool AddPeer(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return false;

            lock (_lock) { return _peers.Add(uri.GetLeftPart(UriPartial.Authority)); }
        }

        // ---------- transactions ----------
        public async Task<bool> SubmitTransactionAsync(Transaction tx)
        {
            bool accepted;
            lock (_lock) { accepted = _blockchain.TryAddTransaction(tx); }

            // Gossip: only a NEW transaction is relayed, so it stops once every node knows it.
            if (accepted)
                await ForEachPeerAsync((client, peer) => client.PostAsJsonAsync($"{peer}/transactions", tx));

            return accepted;
        }

        public async Task<bool> SendFromWalletAsync(string to, decimal amount)
        {
            var tx = Wallet.CreateTransaction(to, amount);
            return await SubmitTransactionAsync(tx);
        }
        // ---------- mining and consensus ----------
        public async Task<Block> MineAsync()
        {
            Block block;

            // CPU-bound: other requests wait while the nonce search runs (fine a learning project).
            lock (_lock) { block = _blockchain.MinePendingTransactions(Wallet.Address); }

            // Tell the peers there is (probably) a longer chain out there.
            await ForEachPeerAsync((client, peer) => client.PostAsync($"{peer}/resolve", content: null));

            return block;
        }

        public async Task<bool> ResolveAsync()
        {
            var replaced = false;

            foreach (var peer in GetPeers())
            {
                try
                {
                    using var client = CreateClient();
                    var chain = await client.GetFromJsonAsync<List<Block>>($"{peer}/chain");
                    if (chain is null) continue;

                    // Network I/O stays outside the lock; only the Core call is inside it.
                    lock (_lock) { replaced |= _blockchain.TryReplaceChain(chain); }
                }
                catch (Exception ex)
                {
                    // Unreachable, slow, or sending garbage: either way it is not our problem.
                    _logger.LogWarning("Peer {Peer} skipped: {Reason}", peer, ex.Message);
                }
            }
            return replaced;
        }

        // ---------- helpers ----------
        private HttpClient CreateClient()
        {
            var client = _httpFactory.CreateClient();
            client.Timeout = PeerTimeout;
            return client;
        }

        private async Task ForEachPeerAsync(Func<HttpClient, string, Task> action)
        {
            foreach (var peer in GetPeers())
            {
                try
                {
                    using var client = CreateClient();
                    await action(client, peer);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Peer {Peer} unreachable: {Reason}", peer, ex.Message);
                }
            }
        }

    }
}