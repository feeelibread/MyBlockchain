using MyBlockchain.Core;
using MyBlockchain.Node;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSingleton(sp =>
{
    // All nodes of the same network MUST use the same difficulty and reward,
    // otherwise they build different genesis blocks and reject each other's chains.
    var config = sp.GetRequiredService<IConfiguration>();
    var difficulty = config.GetValue("Blockchain:Difficulty", 3);
    var reward = config.GetValue("Blockchain:BlockReward", 50m);

    return new BlockchainNode(
        new Blockchain(difficulty, reward),
        new Wallet(),
        sp.GetRequiredService<IHttpClientFactory>(),
        sp.GetRequiredService<ILogger<BlockchainNode>>());
});

var app = builder.Build();
var node = app.Services.GetRequiredService<BlockchainNode>();

// ---- Read ----
app.MapGet("/status", () => node.GetStatus());
app.MapGet("/chain", () => node.GetChain());
app.MapGet("/pending", () => node.GetPending());
app.MapGet("/balance/{address}", (string address) => new { address, balance = node.GetBalance(address) });
app.MapGet("/wallet", () => new { address = node.Wallet.Address, balance = node.GetBalance(node.Wallet.Address) });
app.MapGet("/peers", () => node.GetPeers());

// ---- Write ----
// A signed transaction coming from a client or from another node (gossip).
app.MapPost("/transactions", async (Transaction tx) =>
    await node.SubmitTransactionAsync(tx)
        ? Results.Accepted()
        : Results.BadRequest(new { error = "Transaction rejected (invalid, unsigned, duplicated or without funds)." }));

// Demo convenience: the node signs with its own wallet.
app.MapPost("/wallet/send", async (SendRequest request) =>
    await node.SendFromWalletAsync(request.To, request.Amount)
        ? Results.Accepted()
        : Results.BadRequest(new { error = "Transaction rejected (check recipient, amount and balance)." }));

app.MapPost("/mine", async () =>
{
    var block = await node.MineAsync();
    return Results.Ok(new { block.Index, block.Hash, block.Nonce, transactions = block.Transactions.Count });
});

app.MapPost("/peers", (AddPeerRequest request) =>
    node.AddPeer(request.Url)
        ? Results.Ok(node.GetPeers())
        : Results.BadRequest(new { error = "Invalid URL or peer already registered." }));

// Consensus: fetch the peers' chains and keep the longest valid one.
app.MapPost("/resolve", async () =>
{
    var replaced = await node.ResolveAsync();
    return Results.Ok(new { replaced, length = node.GetChain().Count });
});

app.Run();

record SendRequest(string To, decimal Amount);
record AddPeerRequest(string Url);