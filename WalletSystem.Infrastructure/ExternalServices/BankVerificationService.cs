using System.Net.Http.Json;
using WalletSystem.Core.DTOs.Bank;
using WalletSystem.Core.Interfaces.Services;

namespace WalletSystem.Infrastructure.ExternalServices
{
    public class BankVerificationService : IBankVerificationService
    {
        private readonly HttpClient _client;
        public BankVerificationService(HttpClient client)
        {
            _client = client;
        }

        public Task<BankOperationResponse> CreditAsync(Guid externalBankAccountId, decimal amount)
        {
            throw new NotImplementedException();
        }

        public Task<BankOperationResponse> DebitAsync(Guid externalBankAccountId, decimal amount)
        {
            throw new NotImplementedException();
        }

        public Task<CheckBalanceResponse> GetBalanceAsync(Guid externalBankAccountId)
        {
            throw new NotImplementedException();
        }

        public Task<LinkAccountResult> LinkAsync(string verificationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<VerifyBankResponse> VerifyAccountAsync(VerifyBankRequest request)
        {
            var response = await _client.PostAsJsonAsync("api/bank/accounts/verify", request);
            if (!response.IsSuccessStatusCode)
            {
                return new VerifyBankResponse
                {
                    IsValid = false,
                    Message = "Bank Verfication failed"
                };
            }
            
            var res = await response.Content.ReadFromJsonAsync<VerifyBankResponse>();
            
            if(res == null )
            {
                return new VerifyBankResponse
                {
                    IsValid = false,
                    Message = "Error: Content not found in the bank Verification"
                };
            }

            return res;
        }

     
    }
}
