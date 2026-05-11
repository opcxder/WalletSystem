using SimulatedBank.Enums;

namespace SimulatedBank.Dtos
{
    public class OperationResponse
    {

        public bool Success { get; set; }
        public string? Message { get; set; }

        public Guid? TransactionId { get; set; }

        public BankErrorCode ErrorCode { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public bool IsIdempotentReplay { get; set; }

        public Guid? ExternalReferenceId { get; set; }



    }
}
