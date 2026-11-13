using MyBlockchain.Core;

namespace MyBlockchain.Tests
{
    internal static class TestChains
    {
        public static Blockchain Funded(int difficulty = 1)
        {
            //Chain where "Felipe" already has 100 confirmed coins (temporary faucet).
            var bc = new Blockchain(difficulty);
            bc.TryAddTransaction(new Transaction(Transaction.SystemAddress, "Felipe", 100));
            bc.MinePendingTransactions();
            return bc;
        }
    }
}