using Microsoft.EntityFrameworkCore;
using SimulatedBank.Data;
using SimulatedBank.Dtos;
using SimulatedBank.Entities;


namespace SimulatedBank.Services
{
    public class BankService
    {
        private readonly BankContext _bankContext;

        public BankService(BankContext bankContext) {
            _bankContext = bankContext;
        }


        public async Task<VerifyAccountResponse> VerifyAndLinkAccount(VerifyAccount verifyAccount)
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
                .FirstOrDefaultAsync(b => b.AccountNumber == verifyAccount.AccountNumber);

            if (account == null)
            {
                return new VerifyAccountResponse
                {
                    IsValid = false,
                    Message = "Account not found"
                };
            }

           
            var verify = VerifyAccountHolder(account, verifyAccount);

            if (!verify.IsValid)
                return verify;


            var link = await LinkAccount(account);

            if (!link)
            {
                verify.IsValid = false;
                verify.Message = "Unable to link the account";
                return verify;
            }

         
            verify.Message = "Account verified and linked successfully";
            verify.ExternalBankAccountId = account.ExternalBankAccountId;

            return verify;
        }

        public VerifyAccountResponse VerifyAccountHolder(BankAccount account, VerifyAccount verifyAccount)
        {
            if (account == null || account.Bank == null)
            {
                return new VerifyAccountResponse
                {
                    IsValid = false,
                    Message = "Account Details Not Found"
                };
            }

            if (!string.Equals(verifyAccount.AccountHolderName, account.AccountHolderName, StringComparison.OrdinalIgnoreCase))
            {
                return new VerifyAccountResponse
                {
                    IsValid = false,
                    Message = "Account Details Not Found"
                };
            }

            if (verifyAccount.AccountType != account.AccountType)
            {
                return new VerifyAccountResponse
                {
                    IsValid = false,
                    Message = "Account Details Not Found"
                };
            }

            if (account.Bank.IFSCCode == null || account.Bank.IFSCCode != verifyAccount.IFSCCode)
            {
                return new VerifyAccountResponse
                {
                    IsValid = false,
                    Message = "Account Details Not Found"
                };
            }

            if (!account.IsActive)
            {
                return new VerifyAccountResponse
                {
                    IsValid = false,
                    Message = "Account is not active"
                };
            }

            string acc = account.AccountNumber;
            string masked = acc.Length > 4
                ? new string('*', acc.Length - 4) + acc[^4..]
                : acc;

            return new VerifyAccountResponse
            {
                IsValid = true,
                AccountHolderName = account.AccountHolderName,
                MaskedAccountNumber = masked,
                AccountType = account.AccountType,
                IFSCCode = account.Bank.IFSCCode,
                Message = "Account Details Found"
            };
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
    }
}
