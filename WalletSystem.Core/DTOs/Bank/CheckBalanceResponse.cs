

namespace WalletSystem.Core.DTOs.Bank
{


        public class CheckBalanceResponse
        {
            public bool Success { get; set; }
            public decimal? Balance { get; set; }
            public string? Message { get; set; }
        }

}
