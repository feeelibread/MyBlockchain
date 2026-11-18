using MyBlockchain.Core;

namespace MyBlockchain.Tests
{
    internal static class TestChains
    {
        public static Blockchain Funded(Wallet owner, int difficulty = 1)
        {
            // Chain where "owner" already has confirmmed 100 coins (temporary faucet).
            var bc = new Blockchain(difficulty);
            bc.TryAddTransaction(new Transaction(Transaction.SystemAddress, owner.Address, 100));
            bc.MinePendingTransactions();
            return bc;
        }
    }
}