using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalletSystem.Core.DTOs.Transactions
{
    public   sealed class CommitWalletResult
    {
        public bool Success { get; set; }

        public string? Message { get; set; }
    }
}
