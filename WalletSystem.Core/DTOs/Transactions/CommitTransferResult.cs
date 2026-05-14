
namespace WalletSystem.Core.DTOs.Transactions
{
    public class CommitTransferResult
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public bool Retryable { get; set; }
    }
}
