using MyBlockchain.Core;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

var felipe = new Wallet();
var victor = new Wallet();
var robert = new Wallet();

var bc = new Blockchain(difficulty: 3, blockReward: 50);

// Felipe mines the first block and earns the rewards: coins are born here.
bc.MinePendingTransactions(felipe.Address);
Console.WriteLine($"Felipe mined the block 1: Felipe: {bc.GetBalance(felipe.Address)}");

// Felipe pays Victor; Robert mines the block that confirms it and earns the next reward.
bc.TryAddTransaction(felipe.CreateTransaction(victor.Address, 20));
bc.MinePendingTransactions(robert.Address);



Console.WriteLine();
Console.WriteLine($"Felipe: {bc.GetBalance(felipe.Address)} | Victor: {bc.GetBalance(victor.Address)} | Robert: {bc.GetBalance(robert.Address)}");
Console.WriteLine($"Total supply: {bc.GetBalance(felipe.Address) + bc.GetBalance(victor.Address) + bc.GetBalance(robert.Address)} (= 2 blocks x 50)");
Console.WriteLine($"Is Valid? {bc.IsValid()}");

Console.WriteLine();
Console.WriteLine($"Minting via mempool: {bc.TryAddTransaction(new Transaction(Transaction.SystemAddress, victor.Address, 1000))}");

// Robert (the last miner) pays himself 1000 coins and re-mines: hash and PoW are valid again.

var last = bc.Last;
last.Transactions[0] = last.Transactions[0] with { Amount = 1000 };
last.Mine(bc.Difficulty);
Console.WriteLine($"Miner pays itself 1000 and re-mines, valid?: {bc.IsValid()}");
