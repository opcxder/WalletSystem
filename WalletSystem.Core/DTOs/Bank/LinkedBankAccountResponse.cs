

namespace WalletSystem.Core.DTOs.Bank
{
    public class LinkedBankAccountResponse
    {
        public Guid ExternalReferenceId { get; set; }
        public string MaskedAccountNumber { get; set; }
        public string AccountHolderName { get; set; }

       
        public string BankName { get; set; }
        public string IFSCCode { get; set; }
    }
}
