using MyBlockchain.Core;
using System.Diagnostics;


var bc = new Blockchain(difficulty: 3);

//Temporary "faucet".
bc.TryAddTransaction(new(Transaction.SystemAddress, "Felipe", 100));
bc.MinePendingTransactions();

Console.WriteLine($"Felipe -> Victor 30: {bc.TryAddTransaction(new("Felipe", "Victor", 30))}");
Console.WriteLine($"Victor -> Robert 50: {bc.TryAddTransaction(new("Victor", "Robert", 50))} (Victor has no confirmed coins yet)");
bc.MinePendingTransactions();

Console.WriteLine();

foreach (var name in new[] { "Felipe", "Victor", "Robert" })
    Console.WriteLine($"{name,-7} balance: {bc.GetBalance(name)}");

Console.WriteLine($"\nValid? {bc.IsValid()}");

bc.Chain[1].Transactions[0] = new Transaction(Transaction.SystemAddress, "Felipe", 1_000_000);
Console.WriteLine($"After Tampering: {bc.IsValid()}");