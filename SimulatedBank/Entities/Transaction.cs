

using SimulatedBank.Enums;
using System.ComponentModel.DataAnnotations;

namespace SimulatedBank.Entities
{

    public class Transaction
    {
        public Guid TransactionId { get; private set; }

        public Guid BankAccountId { get; private set; }

        public decimal Amount { get; private set; }

        public TransactionType Type { get; private set; }

        public string Description { get; private set; }

        public DateTime CreatedAt { get; private set; }


        public BankTransactionStatus Status { get; set; }

        public string? FailureReason { get; set; }


        public  BankErrorCode ErrorCode {get;set;}
        
        public DateTime? CompletedAt { get; set; }

        public Guid ExternalReferenceId { get; private   set; }   


        private Transaction() { }

        public static Transaction CreateCredit(Guid accountId, decimal amount, string description, Guid externalReferenceId)
        {
            return new Transaction
            {
                TransactionId = Guid.NewGuid(),
                BankAccountId = accountId,
                Amount = amount,
                Type = TransactionType.Credit,
                Description = description,
                ExternalReferenceId = externalReferenceId,


                Status = BankTransactionStatus.Initiated,
                
                CreatedAt = DateTime.UtcNow,
                    
            };
        }

        public static Transaction CreateDebit(Guid accountId, decimal amount, string description, Guid externalReferenceId)
        {
            return new Transaction
            {
                TransactionId = Guid.NewGuid(),
                BankAccountId = accountId,
                Amount = amount,
                Type = TransactionType.Debit,
                Description = description,

                ExternalReferenceId = externalReferenceId,
                Status = BankTransactionStatus.Initiated,

                CreatedAt = DateTime.UtcNow
            };
        }

        public void MarkSuccess()
        {
            Status = BankTransactionStatus.Success;
            CompletedAt = DateTime.UtcNow;
            ErrorCode = BankErrorCode.None;
        }

        public void MarkFailed(BankErrorCode errorCode, string reason)
        {
            Status = BankTransactionStatus.Failed;
            ErrorCode = errorCode;
            FailureReason = reason;
            CompletedAt = DateTime.UtcNow;
        }
    }
}