namespace SimulatedBank.Entities
{
    public class VerificationToken
    {
        public Guid TokenId { get; set; }
        public string TokenHash { get; set; } = string.Empty;

        public Guid BankAccountId { get; set; }
        public BankAccount BankAccount { get; set; }


        public DateTime ExpiryDate { get; set; }   = DateTime.UtcNow.AddMinutes(5);

        public bool  IsUsed { get; set; } =  false;

        public  DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
