namespace SimulatedBank.Dtos
{
    public class OperationReponse
    {

        public bool success { get; set; }
        public string? message { get; set; }

        public Guid TransactionId { get; set; }



    }
}
