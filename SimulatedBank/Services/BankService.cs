using Microsoft.EntityFrameworkCore;
using SimulatedBank.Data;
using SimulatedBank.Dtos;
using SimulatedBank.Entities;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Threading.Tasks;


namespace SimulatedBank.Services
{
    public class BankService
    {
        private readonly BankContext _bankContext;

        public BankService(BankContext bankContext) {
            _bankContext = bankContext;
        }

        public async Task<VerifyAccountResponse> VerifyAccountHolder(VerifyAccount verifyAccount)
        {
            if (verifyAccount == null ||
                string.IsNullOrWhiteSpace(verifyAccount.AccountNumber) ||
                string.IsNullOrWhiteSpace(verifyAccount.AccountHolderName) ||
                string.IsNullOrWhiteSpace(verifyAccount.IFSCCode))
            {
                return new VerifyAccountResponse
                {
                    IsValid = false,
                    Message = "Account Details Missing"
                };
            }

            var account = await _bankContext.BankAccounts
                .Include(b => b.Bank)
                .FirstOrDefaultAsync(bc =>
                    bc.AccountNumber == verifyAccount.AccountNumber &&
                    bc.Bank.IFSCCode == verifyAccount.IFSCCode);

            if (account == null || account.Bank == null)
            {
                return new VerifyAccountResponse
                {
                    IsValid = false,
                    Message = "Account Details Not Found"
                };
            }

            if (!string.Equals(verifyAccount.AccountHolderName, account.AccountHolderName, StringComparison.OrdinalIgnoreCase) ||
                verifyAccount.AccountType != account.AccountType ||
                !account.IsActive)
            {
                return new VerifyAccountResponse
                {
                    IsValid = false,
                    Message = "Account Details Not Valid"
                };
            }

            string acc = account.AccountNumber;
            string masked = acc.Length > 4
                ? new string('*', acc.Length - 4) + acc[^4..]
                : acc;

            var token = GenerateToken();
            var tokenHash = HashToken(token);

            var verification = new VerificationToken
            {
                TokenId = Guid.NewGuid(),
                BankAccountId = account.BankAccountId,
                TokenHash = tokenHash,
                ExpiryDate = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false
            };

            await _bankContext.VerificationTokens.AddAsync(verification);

            try
            {
                await _bankContext.SaveChangesAsync();
            }
            catch
            {
                return new VerifyAccountResponse
                {
                    IsValid = false,
                    Message = "Unable to create token"
                };
            }

            return new VerifyAccountResponse
            {
                IsValid = true,
                AccountHolderName = account.AccountHolderName,
                MaskedAccountNumber = masked,
                AccountType = account.AccountType,
                IFSCCode = account.Bank.IFSCCode,
                Message = "Account Verified",
                VerificationToken = token
            };
        }

        public async Task<LinkReponse> LinkAccount(LinkRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.VerificationToken))
            {
                return new LinkReponse
                {
                    Success = false,
                    Message = "Invalid request"
                };
            }

            var tokenHash = HashToken(request.VerificationToken);

            var token = await _bankContext.VerificationTokens
                .Include(v => v.BankAccount)
                .FirstOrDefaultAsync(v =>
                    v.TokenHash == tokenHash &&
                    !v.IsUsed &&
                    v.ExpiryDate > DateTime.UtcNow);

            if (token == null)
            {
                return new LinkReponse
                {
                    Success = false,
                    Message = "Invalid or expired token"
                };
            }

            using var tx = await _bankContext.Database.BeginTransactionAsync();

            try
            {
                var account = token.BankAccount;

                // Already linked case
                if (account.ExternalBankAccountId != null)
                {
                    token.IsUsed = true;
                    await _bankContext.SaveChangesAsync();
                    await tx.CommitAsync();

                    return new LinkReponse
                    {
                        Success = true,
                        Message = "Account already linked",
                        ExternalReferenceId = account.ExternalBankAccountId
                    };
                }

                var externalId = Guid.NewGuid();

                account.ExternalBankAccountId = externalId;
                token.IsUsed = true;

                await _bankContext.SaveChangesAsync();
                await tx.CommitAsync();

                return new LinkReponse
                {
                    Success = true,
                    Message = "Account linked successfully",
                    ExternalReferenceId = externalId
                };
            }
            catch
            {
                await tx.RollbackAsync();

                return new LinkReponse
                {
                    Success = false,
                    Message = "Linking failed"
                };
            }
        }
        public async Task<CheckBalanceReponse> CheckBalance(string accountNumber)
        {

            if (string.IsNullOrEmpty(accountNumber))
            {
                return new CheckBalanceReponse
                {
                    success = false,
                    message = "Account number not found"
                };
            }

            var res = await _bankContext.BankAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.AccountNumber == accountNumber);
            if (res == null) {
                return new CheckBalanceReponse
                {
                    success = false,
                    message = "Account number not found"
                };
            }
            if (!res.IsActive)
            {
                return new CheckBalanceReponse
                {
                    success = false,
                    message = "Account is inactive"
                };
            }

            return new CheckBalanceReponse
            {
                success = true,
                balance = res.Balance,
            };
        }


        public async Task<OperationReponse> DebitAmount(Guid ExternalReferenceId, decimal amount)
        {
            if (Guid.Empty == ExternalReferenceId)
            {
                return new OperationReponse
                {
                    success = false,
                    message = "Empty Request"
                };
            }

            var account = await _bankContext.BankAccounts.FirstOrDefaultAsync(x => x.ExternalBankAccountId == ExternalReferenceId);

            if (account == null) {
                return new OperationReponse
                {
                    success = false,
                    message = "Account not found"
                };
            }

            if (!account.IsActive)
            {
                return new OperationReponse
                {
                    success = false,
                    message = "Account not active"
                };
            }

            var transaction = account.Debit(amount, "Wallet request for money");


            try
            {
                await _bankContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return new OperationReponse
                {
                    success = false,
                    message = "Transaction failed due to concurrent update. Please retry."
                };
            }

            return new OperationReponse
            {
                success = true,
                message = "Operation done",
                TransactionId = transaction.TransactionId
            };

        }



        public async Task<OperationReponse> CreditAmount(Guid ExternalReferenceId, decimal amount)
        {
            if (Guid.Empty == ExternalReferenceId)
            {
                return new OperationReponse
                {
                    success = false,
                    message = "Empty Request"
                };
            }

            var account = await _bankContext.BankAccounts.FirstOrDefaultAsync(x => x.ExternalBankAccountId == ExternalReferenceId);

            if (account == null)
            {
                return new OperationReponse
                {
                    success = false,
                    message = "Account not found"
                };
            }

            if (!account.IsActive)
            {
                return new OperationReponse
                {
                    success = false,
                    message = "Account not active"
                };
            }

            var transaction = account.Credit(amount, "Wallet request for money");


           try
{
    await _bankContext.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException)
{
    return new OperationReponse
    {
        success = false,
        message = "Transaction failed due to concurrent update. Please retry."
    };
}

return new OperationReponse
{
    success = true,
    message = "Operation done",
    TransactionId = transaction.TransactionId
};
        }


        public async Task<bool> LinkAccount(BankAccount account)
        {
            if (account == null || !account.IsActive)
                return false;

           
            if (account.ExternalBankAccountId != Guid.Empty)
                return true;

            try
            {
                account.ExternalBankAccountId = Guid.NewGuid();
                await _bankContext.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    
      
        private static string GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(7);
            return Convert.ToBase64String(bytes); 
        }
       private static string HashToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Token is empty");

            }
            var sha = SHA256.Create();
            var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }
    }
}
