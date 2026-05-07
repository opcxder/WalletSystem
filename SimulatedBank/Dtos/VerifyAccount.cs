using SimulatedBank.Enums;
using System.ComponentModel.DataAnnotations;

namespace SimulatedBank.Dtos
{
    public class VerifyAccount
    {

        public string AccountHolderName { get; set; }

        public string AccountNumber { get;  set; }

        public string BankName { get; set; }

        public AccountType AccountType { get; set; }

        [StringLength(20)]
        public string IFSCCode { get; set; }
    }
}
