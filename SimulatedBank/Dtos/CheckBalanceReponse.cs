namespace SimulatedBank.Dtos
{
    public class CheckBalanceReponse
    {

        public bool success { get; set; }
        public decimal balance { get; set; }

        public string? message { get; set; }
    }
}
