using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalletSystem.Core.DTOs.Transactions
{
    public sealed class BankDebitResult
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public Guid BankTransactionId { get; set; }

        public bool BankWasDebited { get; set; }



    }
}
