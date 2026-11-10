using MyBlockchain.Core;
using System.Diagnostics;

Console.WriteLine("Mining cost by difficulty:");
for (int difficulty = 1; difficulty <= 5; difficulty++)
{
    var block = new Block(1, "Felipe -> Victor: 10", "0");
    var sw = Stopwatch.StartNew();
    block.Mine(difficulty);
    sw.Stop();

    Console.WriteLine($"  difficulty {difficulty}: nonce={block.Nonce,9:N0}  time={sw.ElapsedMilliseconds,6} ms  hash={block.Hash[..16]}...");
}

Console.WriteLine();
var bc = new Blockchain(difficulty: 4);
bc.AddBlock("Felipe -> Victor: 10");
bc.AddBlock("Thais -> Thayna: 3");

Console.WriteLine($"Valid? {bc.IsValid()}");
bc.Chain[1].Data = "Felipe -> Victor: 100";
Console.WriteLine($"Valid after tampering? {bc.IsValid()}");