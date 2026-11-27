# MyBlockchain

A blockchain built from scratch in C# (.NET 8) to learn how it works.
Each step is a Pull Request and a tag: browse the tags to watch the project grow.

## Roadmap

| Step | Tag | Topic |
|---|---|---|
| 1-2 | `v0.1-chain` | Blocks, hashing, chain validation |
| 3 | `v0.2-pow` | Proof of Work |
| 4 | `v0.3-transactions` | Transactions, mempool, balances |
| 5 | `v0.4-signatures` | Wallets and ECDSA signatures |
| 6 | `v0.5-mining-reward` | Mining rewards and consensus rules |
| 7 | `v0.6-p2p-network` | HTTP nodes, gossip and longest-chain consensus |

## Run

    dotnet test
    dotnet run --project src/MyBlockchain.Demo

### Run a local network

    dotnet run --project src/MyBlockchain.Node --urls http://localhost:5001
    dotnet run --project src/MyBlockchain.Node --urls http://localhost:5002

Then follow `src/MyBlockchain.Node/MyBlockchain.Node.http`.
All nodes must share `--Blockchain:Difficulty` (default 3) and `--Blockchain:BlockReward` (default 50).

## Known limitations

- Nodes exchange the **whole chain**; real networks exchange blocks incrementally.
- "Longest chain" equals "most work" only because the difficulty is fixed.
- Peers are registered by hand; there is no discovery and no authentication.
  Do not expose a node to the internet (`/wallet/send` spends the node's coins).
- Mining holds a lock, so a node does not answer other requests while it mines.
- State lives in memory: restarting a node wipes it (next step).