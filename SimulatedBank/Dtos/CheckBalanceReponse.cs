namespace SimulatedBank.Dtos { 
    public class CheckBalanceReponse
    {

        public bool Success { get; set; }
        public decimal Balance { get; set; }

        public string? Message { get; set; }
    }
}
