namespace SimulatedBank.Entities
{
    public class Bank
    {
        public Guid BankId { get; private set; }

        public string Name { get; private set; }

        public string IFSCCode { get; private set; }

        public List<BankAccount> BankAccounts { get; private set; } = new();

        private Bank() { }

        public Bank(string name, string ifscCode)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Bank name is required");

            if (string.IsNullOrWhiteSpace(ifscCode))
                throw new ArgumentException("IFSC code is required");

            BankId = Guid.NewGuid();
            Name = name;
            IFSCCode = ifscCode;
        }
    }
}
