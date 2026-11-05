using MyBlockchain.Core;

var bc = new Blockchain();
bc.AddBlock("Felipe -> Victor: 10");
bc.AddBlock("Victor -> Robert: 20");

Console.WriteLine($"Valid? {bc.IsValid()}");
foreach (var block in bc.Chain)
    Console.WriteLine($"Index: {block.Index}, Data: {block.Data}, Hash: {block.Hash[..16]}, PreviousHash: {block.PreviousHash[..Math.Min(16, block.PreviousHash.Length)]}");

bc.Chain[1].Data = "Felipe -> Victor: 100";
Console.WriteLine($"After tampering: {bc.IsValid()}");