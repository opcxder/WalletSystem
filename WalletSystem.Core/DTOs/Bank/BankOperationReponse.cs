

namespace WalletSystem.Core.DTOs.Bank
{
    public class BankOperationResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public Guid? TransactionId { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public bool IsIdempotentReplay { get; set; }
        public string? ErrorCode { get; set; }


        public Guid? ExternalReferenceId { get; set; }
    }
}
