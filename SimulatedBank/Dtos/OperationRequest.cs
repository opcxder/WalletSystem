namespace SimulatedBank.Dtos
{
    public class OperationRequest
    {
            public Guid ExternalBankAccountId { get; set; }
        public decimal amount { get; set; }
    }
}
   