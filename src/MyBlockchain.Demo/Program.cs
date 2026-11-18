using MyBlockchain.Core;
using System.Diagnostics;

// Every address starts with the same ASN.1 header, so we show the tail.
static string Short(string address) => "..." + address[^8..];

var felipe = new Wallet();
var victor = new Wallet();
var robert = new Wallet();

var bc = new Blockchain(difficulty: 3);

// Temporary faucet
bc.TryAddTransaction(new Transaction(Transaction.SystemAddress, felipe.Address, 100m));
bc.MinePendingTransactions();

Console.WriteLine($"Felipe's address: {Short(felipe.Address)} ({felipe.Address.Length}) hex chars\n");

var t1 = felipe.CreateTransaction(victor.Address, 20);
Console.WriteLine($"Felipe -> Victor 20 (signed):   {bc.TryAddTransaction(t1)}");

var unsigned = new Transaction(felipe.Address, victor.Address, 20);
Console.WriteLine($"Felipe -> Robert 20 (unsigned):   {bc.TryAddTransaction(unsigned)}");

var forged = victor.CreateTransaction(robert.Address, 50) with { From = felipe.Address };
Console.WriteLine($"Victor forging Felipe's identity:   {bc.TryAddTransaction(forged)}");

Console.WriteLine($"Replaying Felipe -> Victor 20:   {bc.TryAddTransaction(t1)}");

bc.MinePendingTransactions();

Console.WriteLine();

Console.WriteLine($"Felipe: {bc.GetBalance(felipe.Address)} | Victor: {bc.GetBalance(victor.Address)} | Robert: {bc.GetBalance(robert.Address)}");
Console.WriteLine($"Is Valid? {bc.IsValid()}");

// Attacker edits the amount and RE-MINES the block: hash and PoW are valid again.
var last = bc.Last;
last.Transactions[0] = last.Transactions[0] with { Amount = 90 };
last.Mine(bc.Difficulty);
Console.WriteLine($"After tampering + re-mining: {bc.IsValid()} (signature no longer matches)");
