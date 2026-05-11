using System.ComponentModel.DataAnnotations;

namespace SimulatedBank.Dtos
{
    public class OperationRequest
    {
        [Required]
            public Guid ExternalBankAccountId { get; set; }

        [Required]
        [Range(0.01, 10000)]
        public decimal Amount { get; set; }

        [Required]
        public Guid ExternalReferenceId { get; set; }
    }
}
   