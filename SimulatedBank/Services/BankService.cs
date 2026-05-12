using Azure.Core;
using Microsoft.EntityFrameworkCore;
using SimulatedBank.Data;
using SimulatedBank.Dtos;
using SimulatedBank.Entities;
using SimulatedBank.Enums;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Threading.Tasks;


namespace SimulatedBank.Services
{
    public class BankService
    {
        private readonly BankContext _bankContext;
        private readonly ILogger<BankService> _logger;

        public BankService(BankContext bankContext, ILogger<BankService> logger)
        {
            _bankContext = bankContext;
            _logger = logger;
        }

        public async Task<VerifyAccountResponse> VerifyAccountHolder(VerifyAccount verifyAccount, CancellationToken ct)
        {
            if (verifyAccount == null ||
                string.IsNullOrWhiteSpace(verifyAccount.AccountNumber) ||
                string.IsNullOrWhiteSpace(verifyAccount.AccountHolderName) ||
                string.IsNullOrWhiteSpace(verifyAccount.IFSCCode) ||
                string.IsNullOrWhiteSpace(verifyAccount.BankName)
                )
            {
                return new VerifyAccountResponse
                {
                    Success = false,
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
                    Success = false,
                    Message = "Account Details Not Found"
                };
            }

            if (!string.Equals(verifyAccount.AccountHolderName, account.AccountHolderName, StringComparison.OrdinalIgnoreCase) ||
                verifyAccount.AccountType != account.AccountType ||
                !account.IsActive)
            {
                return new VerifyAccountResponse
                {
                    Success = false,
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

            await _bankContext.VerificationTokens.AddAsync(verification, ct);

            try
            {
                await _bankContext.SaveChangesAsync(ct);
            }
            catch
            {
                return new VerifyAccountResponse
                {
                    Success = false,
                    Message = "Unable to create token"
                };
            }

            return new VerifyAccountResponse
            {
                Success = true,
                AccountHolderName = account.AccountHolderName,
                MaskedAccountNumber = masked,
                AccountType = account.AccountType,
                BankName = account.BankName,
                IFSCCode = account.Bank.IFSCCode,
                Message = "Account Verified",
                VerificationToken = token
            };
        }

        public async Task<LinkReponse> LinkAccount(LinkRequest request, CancellationToken ct)
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
                if (account.ExternalBankAccountId.HasValue &&
                    account.ExternalBankAccountId.Value != Guid.Empty)
                {
                    token.IsUsed = true;
                    await _bankContext.SaveChangesAsync(ct);
                    await tx.CommitAsync();

                    return new LinkReponse
                    {
                        Success = true,
                        Message = "Account already linked",
                        ExternalReferenceId = account.ExternalBankAccountId
                    };
                }

                var externalId = Guid.NewGuid();
                _logger.LogInformation("External id we have created: {id}", externalId);
                account.ExternalBankAccountId = externalId;
                _logger.LogInformation("External id we have assigned to the account: {id}", account.ExternalBankAccountId);
                token.IsUsed = true;

                await _bankContext.SaveChangesAsync(ct);
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
        public async Task<CheckBalanceReponse> CheckBalance(Guid externalRefernceId, CancellationToken ct)
        {

            if (externalRefernceId == Guid.Empty)
            {
                return new CheckBalanceReponse
                {
                    Success = false,
                    Message = "Refernce ID  is required"
                };
            }

            var res = await _bankContext.BankAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.ExternalBankAccountId == externalRefernceId, ct);
            if (res == null)
            {
                return new CheckBalanceReponse
                {
                    Success = false,
                    Message = "Refernce ID not found"
                };
            }
            if (!res.IsActive)
            {
                return new CheckBalanceReponse
                {
                    Success = false,
                    Message = "Account is inactive"
                };
            }

            return new CheckBalanceReponse
            {
                Success = true,
                Balance = res.Balance,
            };
        }


        public async Task<OperationResponse> DebitAmount(Guid externalBankAccountId, decimal amount, Guid externalReferenceId, CancellationToken ct = default)
        {
            if (externalBankAccountId == Guid.Empty || externalReferenceId == Guid.Empty)
            {
                return new OperationResponse
                {
                    Success = false,
                    Message = "Invalid request",
                    ErrorCode = BankErrorCode.InvalidRequest,
                    ProcessedAt = DateTime.UtcNow
                };
            }

            // Idempotency check
            var existing = await _bankContext.Transactions
                   .FirstOrDefaultAsync(t => t.ExternalReferenceId == externalReferenceId && t.Type == TransactionType.Debit, ct);


            var account = await _bankContext.BankAccounts
            .FirstOrDefaultAsync(x => x.ExternalBankAccountId == externalBankAccountId, ct);

            if (account == null)
            {
                return new OperationResponse
                {
                    Success = false,
                    Message = "Account not found",
                    ErrorCode = BankErrorCode.AccountNotFound,
                    ProcessedAt = DateTime.UtcNow
                };
            }

            if (!account.IsActive)
            {
                return new OperationResponse
                {
                    Success = false,
                    Message = "Account not active",
                    ErrorCode = BankErrorCode.AccountInActive,
                    ProcessedAt = DateTime.UtcNow
                };
            }


            if (existing != null)
            {
                if (existing.BankAccountId != account.BankAccountId ||
                     existing.Amount != amount)
                {
                    return new OperationResponse
                    {
                        Success = false,
                        Message = "Idempotency key reused with different request data",
                        ErrorCode = BankErrorCode.DuplicateRequest,
                        TransactionId = existing.TransactionId,
                        ExternalReferenceId = existing.ExternalReferenceId,
                        IsIdempotentReplay = true,
                        ProcessedAt = existing.CompletedAt
                    };
                }

                return new OperationResponse
                {
                    Success = existing.Status == BankTransactionStatus.Success,
                    TransactionId = existing.TransactionId,
                    ExternalReferenceId = existing.ExternalReferenceId,
                    ProcessedAt = existing.CompletedAt,
                    ErrorCode = existing.ErrorCode,
                    IsIdempotentReplay = true
                };
            }

           

           

            try
            {
                var transaction = account.Debit(amount, "Wallet debit request", externalReferenceId);

                transaction.MarkSuccess();


                await _bankContext.SaveChangesAsync(ct);

                return new OperationResponse
                {
                    Success = true,
                    TransactionId = transaction.TransactionId,
                    ExternalReferenceId = externalReferenceId,
                    ProcessedAt = transaction.CompletedAt,
                    ErrorCode = BankErrorCode.None,
                    IsIdempotentReplay = false
                };
            }
            catch (DbUpdateConcurrencyException)
            {
                return new OperationResponse
                {
                    Success = false,
                    Message = "Concurrent request conflict. Please retry.",
                    ErrorCode = BankErrorCode.ConcurrencyConflict,
                    ProcessedAt = DateTime.UtcNow
                };
            }
            catch (InvalidOperationException ex)
            {
                return new OperationResponse
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = BankErrorCode.InsufficientFunds,
                    ProcessedAt = DateTime.UtcNow
                };
            }
            catch (DbUpdateException)
            {
                return new OperationResponse
                {
                    Success = false,
                    Message = "Duplicate or conflicting request",
                    ErrorCode = BankErrorCode.DuplicateRequest,
                    ProcessedAt = DateTime.UtcNow
                };
            }
            catch (Exception)
            {
                return new OperationResponse
                {
                    Success = false,
                    Message = "Internal server error",
                    ErrorCode = BankErrorCode.InternalError,
                    ProcessedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<OperationResponse> CreditAmount(Guid externalBankAccountId, decimal amount, Guid externalReferenceId, CancellationToken ct = default)
        {
            if (externalBankAccountId == Guid.Empty || externalReferenceId == Guid.Empty)
            {
                return new OperationResponse
                {
                    Success = false,
                    Message = "Invalid request",
                    ErrorCode = BankErrorCode.InvalidRequest,
                    ProcessedAt = DateTime.UtcNow
                };
            }

            // Idempotency check
            var existing = await _bankContext.Transactions
                   .FirstOrDefaultAsync(t => t.ExternalReferenceId == externalReferenceId && t.Type == TransactionType.Credit, ct);


            var account = await _bankContext.BankAccounts
            .FirstOrDefaultAsync(x => x.ExternalBankAccountId == externalBankAccountId,ct);

            if (account == null)
            {
                return new OperationResponse
                {
                    Success = false,
                    Message = "Account not found",
                    ErrorCode = BankErrorCode.AccountNotFound,
                    ProcessedAt = DateTime.UtcNow
                };
            }

            if (!account.IsActive)
            {
                return new OperationResponse
                {
                    Success = false,
                    Message = "Account not active",
                    ErrorCode = BankErrorCode.AccountInActive,
                    ProcessedAt = DateTime.UtcNow
                };
            }


            if (existing != null)
            {
                if (existing.BankAccountId != account.BankAccountId ||
                    existing.Amount != amount)
                {
                    return new OperationResponse
                    {
                        Success = false,
                        Message = "Idempotency key reused with different request data",
                        ErrorCode = BankErrorCode.DuplicateRequest,
                        TransactionId = existing.TransactionId,
                        ExternalReferenceId = existing.ExternalReferenceId,
                        IsIdempotentReplay = true,
                        ProcessedAt = existing.CompletedAt
                    };
                }

                return new OperationResponse
                {
                    Success = existing.Status == BankTransactionStatus.Success,
                    TransactionId = existing.TransactionId,
                    ExternalReferenceId = existing.ExternalReferenceId,
                    ProcessedAt = existing.CompletedAt,
                    ErrorCode = existing.ErrorCode,
                    IsIdempotentReplay = true
                };
            }

        

            try
            {
                var transaction = account.Credit(amount, "Wallet credit request", externalReferenceId);

                transaction.MarkSuccess();


                await _bankContext.SaveChangesAsync(ct);

                return new OperationResponse
                {
                    Success = true,
                    TransactionId = transaction.TransactionId,
                    ExternalReferenceId = externalReferenceId,
                    ProcessedAt = transaction.CompletedAt,
                    ErrorCode = BankErrorCode.None,
                    IsIdempotentReplay = false
                };
            }
            catch (DbUpdateConcurrencyException)
            {
                return new OperationResponse
                {
                    Success = false,
                    Message = "Concurrent request conflict. Please retry.",
                    ErrorCode = BankErrorCode.ConcurrencyConflict,
                    ProcessedAt = DateTime.UtcNow
                };
            }
            catch (InvalidOperationException ex)
            {
                return new OperationResponse
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = BankErrorCode.InvalidAmount,
                    ProcessedAt = DateTime.UtcNow
                };
            }
            catch (DbUpdateException)
            {
                return new OperationResponse
                {
                    Success = false,
                    Message = "Duplicate or conflicting request",
                    ErrorCode = BankErrorCode.DuplicateRequest,
                    ProcessedAt = DateTime.UtcNow
                };
            }
            catch (Exception)
            {
                return new OperationResponse
                {
                    Success = false,
                    Message = "Internal server error",
                    ErrorCode = BankErrorCode.InternalError,
                    ProcessedAt = DateTime.UtcNow
                };
            }
        }



        //public async Task<bool> LinkAccount(BankAccount account, CancellationToken ct)
        //{
        //    if (account == null || !account.IsActive)
        //        return false;


        //    if (account.ExternalBankAccountId != Guid.Empty)
        //        return true;

        //    try
        //    {
        //        account.ExternalBankAccountId = Guid.NewGuid();
        //        await _bankContext.SaveChangesAsync(ct);
        //        return true;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}


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
