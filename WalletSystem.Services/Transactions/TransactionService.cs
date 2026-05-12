
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WalletSystem.Core.common;
using WalletSystem.Core.DTOs.Transactions;
using WalletSystem.Core.DTOs.Wallets;
using WalletSystem.Core.Entities;
using WalletSystem.Core.Enums;
using WalletSystem.Core.Interfaces.Repositories;
using WalletSystem.Core.Interfaces.Services;
using WalletSystem.Infrastructure.Data;

namespace WalletSystem.Services.Transactions
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILinkedBankAccountRepository _linkedBankAccountRepository;
        private readonly IVpaRepository _vpaRepository;
        private readonly WalletContext _walletContext;
        private readonly IUnitOfWork _unitOfWork;

        private readonly IBankVerificationService _bankVerificationService;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(ITransactionRepository transactionRepository,
                                  ILogger<TransactionService> logger,
                                  IWalletRepository walletRepository,
                                  IUserRepository userRepository,
                                  IBankVerificationService bankVerificationService,
                                  ILinkedBankAccountRepository linkedBankAccountRepository,
                                  IVpaRepository vpaRepository,
                                  WalletContext walletContext,
                                  IUnitOfWork unitOfWork
             )
        {
            _transactionRepository = transactionRepository;
            _walletRepository = walletRepository;
            _userRepository = userRepository;
            _bankVerificationService = bankVerificationService;
            _linkedBankAccountRepository = linkedBankAccountRepository;
            _vpaRepository = vpaRepository;
            _walletContext = walletContext;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }


        public async Task<ServiceResult<TransactionResponse>> AddMoneyAsync(
            Guid userId, AddMoneyRequest request, CancellationToken ct = default)
        {
            try
            {
                //validation and prepare the context
                var validationResult =
                    await ValidateAndPrepareAsync(userId, request, ct);

                if (!validationResult.Success)
                {
                    return ServiceResult<TransactionResponse>
                        .Fail(validationResult.Message!);
                }

                // idempotent replay response
                if (validationResult.IdempotentResponse != null)
                {
                    return ServiceResult<TransactionResponse>
                        .Ok(validationResult.IdempotentResponse);
                }

                var context = validationResult.Context!;


                try
                {
                    await _transactionRepository.AddAsync(context.Transaction, ct);
                    await _unitOfWork.SaveChangesAsync(ct);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogWarning(ex, "Add-money transaction insert failed. Checking for idempotent replay.");

                    var existingTransaction = await _transactionRepository.GetAddMoneyByIdempotencyAsync(
                        context.Wallet.WalletId,
                        context.LinkedBankAccount.ExternalBankAccountId,
                        context.Transaction.IdempotencyKey,
                        ct);

                    if (existingTransaction != null &&
                        existingTransaction.Amount == context.Transaction.Amount)
                    {
                        return ServiceResult<TransactionResponse>.Ok(new TransactionResponse
                        {
                            TransactionId = existingTransaction.TransactionId,
                            Amount = existingTransaction.Amount,
                            Status = existingTransaction.Status,
                            Type = existingTransaction.Type,
                            CreatedAt = existingTransaction.CreatedAt,
                            ReferenceId = existingTransaction.ReferenceId!,
                            IsIdempotentReplay = true,
                            FailureReason = existingTransaction.FailureReason ?? existingTransaction.CompensationFailureReason
                        });
                    }
                    if (existingTransaction != null &&
                        existingTransaction.Amount != context.Transaction.Amount)
                    {
                        return ServiceResult<TransactionResponse>.Fail(
                       "Transaction already exists with different request data.");
                    }

                    if (existingTransaction == null)
                    {
                        return ServiceResult<TransactionResponse>.Fail("Unable to create transaction. Please retry.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create add-money transaction");
                    return ServiceResult<TransactionResponse>.Fail("Unable to create transaction. Please retry.");
                }




                //external bank call
                var debitResult =
                    await DebitBankAsync(context, ct);

                if (!debitResult.Success)
                {
                    if (debitResult.BankWasDebited)
                    {
                        var compensationResult = await CompensateBankCreditAsync(context, ct);

                        return ServiceResult<TransactionResponse>.Fail(compensationResult.Message ?? "Wallet credit failed after bank debit");
                    }

                    context.Transaction.MarkFailed(debitResult.Message ?? "Bank debit failed");
                    await _unitOfWork.SaveChangesAsync(ct);

                    return ServiceResult<TransactionResponse>.Fail(debitResult.Message ?? "Bank debit failed");
                }


                context.BankTransactionId = debitResult.BankTransactionId;

                //db commit and credit wallet
                var commitResult =
                    await CommitWalletCreditAsync(context, ct);

                if (!commitResult.Success)
                {
                    //compensation to the bank
                    var compensationResult = await CompensateBankCreditAsync(context, ct);

                    return ServiceResult<TransactionResponse>.Fail(
                        compensationResult.Message ?? commitResult.Message ?? "Wallet credit failed after bank debit");

                }

                return ServiceResult<TransactionResponse>.Ok(
                    new TransactionResponse
                    {
                        TransactionId = context.Transaction.TransactionId,
                        Amount = context.Transaction.Amount,
                        Status = context.Transaction.Status,
                        Type = context.Transaction.Type,
                        CreatedAt = context.Transaction.CreatedAt,
                        ReferenceId = context.Transaction.ReferenceId!,
                        IsIdempotentReplay = false,
                        FailureReason = context.Transaction.FailureReason

                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled exception in AddMoneyAsync");

                return ServiceResult<TransactionResponse>
                    .Fail("Unexpected error");
            }
        }




        public async Task<ServiceResult<TransactionResponse>> GetByIdAsync(Guid userId, Guid transactionId, CancellationToken ct = default)
        {
            if (userId == Guid.Empty || transactionId == Guid.Empty)
            {
                _logger.LogWarning("User id or Transaction Id not found while fetching list of transaction");
                return ServiceResult<TransactionResponse>.Fail("Missing Input");
            }

            var result = await _transactionRepository.GetByTransactionIdForUserAsync(userId, transactionId, ct);
            if (result == null)
            {
                _logger.LogWarning("No result received from the Database");
                return ServiceResult<TransactionResponse>.Fail("No Transaction Found");
            }

            var res = new TransactionResponse
            {
                TransactionId = result.TransactionId,
                Amount = result.Amount,
                CreatedAt = result.CreatedAt,
                Type = result.Type,
                ReferenceId = result.ReferenceId ?? " Not Found",
                Status = result.Status,
                FailureReason = result.FailureReason ?? result.CompensationFailureReason

            };

            return ServiceResult<TransactionResponse>.Ok(res);

        }

        public async Task<ServiceResult<List<TransactionResponse>>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        {
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("User id not found while fetching list of transaction");
                return ServiceResult<List<TransactionResponse>>.Fail("Missing Input");

            }

            var result = await _transactionRepository.GetTransactionsForUserAsync(userId, ct);
            if (result == null || !result.Any())
            {
                _logger.LogWarning("No result received from the Database");
                return ServiceResult<List<TransactionResponse>>.Fail("No Transaction Found");
            }


            var response = result.Select(t => new TransactionResponse
            {
                TransactionId = t.TransactionId,
                Amount = t.Amount,
                ReferenceId = t.ReferenceId ?? "Not Found",
                CreatedAt = t.CreatedAt,
                Status = t.Status,
                Type = t.Type,
                FailureReason = t.FailureReason ?? t.CompensationFailureReason


            }).ToList();

            return ServiceResult<List<TransactionResponse>>.Ok(response);
        }

        public Task<ServiceResult<TransactionResponse>> SendMoneyAsync(Guid userId, SendMoneyRequest request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }



        private async Task<AddMoneyValidationResult> ValidateAndPrepareAsync(
        Guid userId, AddMoneyRequest request, CancellationToken ct)
        {
            if (userId == Guid.Empty)
            {
                return new AddMoneyValidationResult
                {
                    Success = false,
                    Message = "Invalid user"
                };
            }

            if (request == null)
            {
                return new AddMoneyValidationResult
                {
                    Success = false,
                    Message = "Invalid request"
                };
            }

            if (request.Amount <= 0 || request.Amount > 10000)
            {
                return new AddMoneyValidationResult
                {
                    Success = false,
                    Message = "Invalid amount"
                };
            }

            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                return new AddMoneyValidationResult
                {
                    Success = false,
                    Message = "Missing idempotency key"
                };
            }

            var normalizedKey =
                request.IdempotencyKey
                    .Trim()
                    .ToLowerInvariant();


            // fetching the user 
            var user =
                await _userRepository
                    .GetByIdAsync(userId, ct);

            if (user == null ||
                user.Status != UserStatus.Active)
            {
                return new AddMoneyValidationResult
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // fetching the wallet
            var wallet =
                await _walletRepository
                    .GetByUserIdForUpdateAsync(userId, ct);

            if (wallet == null ||
                wallet.Status != WalletStatus.Active)
            {
                return new AddMoneyValidationResult
                {
                    Success = false,
                    Message = "Wallet not found"
                };
            }

            // fetching  Bank account 
            var linkedBankAccount = await _linkedBankAccountRepository.GetByUserIdAsync(userId, ct);

            if (linkedBankAccount == null || !linkedBankAccount.IsVerified || linkedBankAccount.IsDeleted)
            {
                return new AddMoneyValidationResult
                {
                    Success = false,
                    Message = "Linked bank account not found"
                };
            }

            //  fetching  the vpa
            var userVpa = await _vpaRepository.GetByWalletIdAsync(wallet.WalletId, ct);

            if (userVpa == null)
            {
                return new AddMoneyValidationResult
                {
                    Success = false,
                    Message = "VPA not found"
                };
            }

            // imdempotency check 
            var existingTransaction =
                await _transactionRepository
                   .GetAddMoneyByIdempotencyAsync(wallet.WalletId, linkedBankAccount.ExternalBankAccountId, normalizedKey, ct);

            if (existingTransaction != null)
            {
                if (existingTransaction.Amount == request.Amount)
                {
                    return new AddMoneyValidationResult
                    {
                        Success = true,
                        IdempotentResponse =
                            new TransactionResponse
                            {
                                TransactionId =
                                    existingTransaction.TransactionId,

                                Amount =
                                    existingTransaction.Amount,

                                Status =
                                    existingTransaction.Status,

                                Type =
                                    existingTransaction.Type,

                                ReferenceId =
                                    existingTransaction.ReferenceId!,


                                IsIdempotentReplay = true,

                                CreatedAt = existingTransaction.CreatedAt,
                                FailureReason = existingTransaction.FailureReason ?? existingTransaction.CompensationFailureReason



                            }
                    };
                }

                return new AddMoneyValidationResult
                {
                    Success = false,
                    Message =
                        "Transaction already exists with different amount"
                };
            }


            // creating the transaction
            var transaction = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                SourceType = SourceType.Bank,
                SourceBankAccountId = linkedBankAccount.ExternalBankAccountId,

                DestinationWalletId = wallet.WalletId,

                DestinationVPA = userVpa.VpaAddress,

                Amount = request.Amount,

                Type = TransactionType.AddMoney,

                Description = "Wallet credit initiated",

                Status = TransactionStatus.Initiated,

                IdempotencyKey = normalizedKey,

                ReferenceId = GenerateReferenceId(),

                CreatedAt = DateTime.UtcNow,

                UpdatedAt = DateTime.UtcNow
            };

            return new AddMoneyValidationResult
            {
                Success = true,

                Context = new AddMoneyContext
                {
                    User = user,
                    Wallet = wallet,
                    LinkedBankAccount = linkedBankAccount,
                    UserVpa = userVpa,
                    Transaction = transaction
                }
            };
        }

        private async Task<BankDebitResult> DebitBankAsync(
        AddMoneyContext context, CancellationToken ct)
        {
            try
            {
                var result =
                    await _bankVerificationService.DebitAsync(
                        context.LinkedBankAccount.ExternalBankAccountId,
                        context.Transaction.Amount,
                         context.Transaction.TransactionId,
                        ct);

                if (!result.Success)
                {
                    return new BankDebitResult
                    {
                        Success = false,
                        BankWasDebited = false,
                        Message = result.Message ?? "Bank debit failed"
                    };
                }

                if (!result.TransactionId.HasValue ||
                    result.TransactionId == Guid.Empty)
                {
                    return new BankDebitResult
                    {
                        Success = false,
                        BankWasDebited = true,
                        Message = "Bank returned invalid transaction id"
                    };
                }

                if (!result.ProcessedAt.HasValue)
                {
                    return new BankDebitResult
                    {
                        Success = false,
                        BankWasDebited = true,
                        Message = "Bank returned empty processed time"
                    };
                }


                context.Transaction.MarkBankDebitSuccess(
                    result.TransactionId.Value,
                    result.ProcessedAt.Value
                    );
                try
                {
                    await _unitOfWork.SaveChangesAsync(ct);

                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to create add-money transaction");
                    return new BankDebitResult
                    {
                        Success = false,
                        BankWasDebited = true,
                        BankTransactionId = result.TransactionId.Value,
                        Message = "Unable to update transaction status after bank debit"

                    };
                }


                return new BankDebitResult
                {
                    Success = true,
                    BankTransactionId = result.TransactionId.Value,
                    BankWasDebited = true,
                    Message = "Transaction Successfull"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bank debit failed");
                context.Transaction.MarkFailed("Bank service unavailable");

                try { await _unitOfWork.SaveChangesAsync(ct); } catch { /* best effort */ }

                return new BankDebitResult { Success = false, Message = "Bank service unavailable" };


            }
        }

        private async Task<CommitWalletResult> CommitWalletCreditAsync(
        AddMoneyContext context, CancellationToken ct, int attempt = 0)
        {
            await using var dbTransaction = await _walletContext.Database.BeginTransactionAsync(ct);
            try
            {


                var balanceBefore = context.Wallet.Balance;
                context.Wallet.ApplyCredit(context.Transaction.Amount);
                var balanceAfter = context.Wallet.Balance;

                await _walletContext.LedgerEntries.AddAsync(new LedgerEntry
                {
                    EntryId = Guid.NewGuid(),
                    WalletId = context.Wallet.WalletId,
                    TransactionId = context.Transaction.TransactionId,
                    EntryType = EntryType.Credit,
                    Amount = context.Transaction.Amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = balanceAfter,
                    CreatedAt = DateTime.UtcNow
                }, ct);

                context.Transaction.MarkSuccess();

                await _unitOfWork.SaveChangesAsync(ct);
                await dbTransaction.CommitAsync(ct);

                return new CommitWalletResult { Success = true };
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await dbTransaction.RollbackAsync(ct);

                _logger.LogWarning(ex,
                    "Wallet credit concurrency conflict for transactionId {TransactionId}",
                    context.Transaction.TransactionId);

                return new CommitWalletResult
                {
                    Success = false,
                    Message = "Wallet update conflict. Bank debit compensation scheduled."
                };
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Wallet commit failed");
                await dbTransaction.RollbackAsync(ct);
                return new CommitWalletResult { Success = false, Message = "Wallet update failed" };
            }
        }
        private async Task<CommitWalletResult> CompensateBankCreditAsync(
                     AddMoneyContext context, CancellationToken ct)
        {
            if (context.Transaction.Status == TransactionStatus.Compensated)
            {
                return new CommitWalletResult
                {
                    Success = true,
                    Message = "Compensation already completed"
                };
            }

            context.Transaction.MarkCompensationPending("Compensation pending because wallet credit could not be completed");

            var compensationPendingSaved = true;

            try
            {
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                compensationPendingSaved = false;

                _logger.LogCritical(ex,
                    "Unable to mark compensation pending for transactionId {TransactionId}. Attempting bank refund anyway.",
                    context.Transaction.TransactionId);
            }

            try
            {
                var result = await _bankVerificationService.CreditAsync(
                    context.LinkedBankAccount.ExternalBankAccountId,
                    context.Transaction.Amount,
                    context.Transaction.TransactionId,
                    ct);

                if (result.Success || result.IsIdempotentReplay)
                {
                    context.Transaction.MarkCompensated();

                    try
                    {
                        await _unitOfWork.SaveChangesAsync(ct);
                    }
                    catch (Exception saveEx)
                    {
                        _logger.LogCritical(saveEx,
                            "Bank refund succeeded but local compensated status could not be saved for transactionId {TransactionId}",
                            context.Transaction.TransactionId);

                        return new CommitWalletResult
                        {
                            Success = true,
                            Message = "Bank debit refunded, but local compensation status could not be saved. Manual review required."
                        };
                    }

                    return new CommitWalletResult
                    {
                        Success = true,
                        Message = compensationPendingSaved
                            ? "Bank debit compensated successfully"
                            : "Bank debit compensated successfully, but pending state could not be saved earlier."
                    };
                }

                context.Transaction.MarkCompensationRetryFailed(
                    result.Message ?? result.ErrorCode ?? "Bank compensation failed");

                try
                {
                    await _unitOfWork.SaveChangesAsync(ct);
                }
                catch (Exception saveEx)
                {
                    _logger.LogCritical(saveEx,
                        "Bank compensation failed and retry state could not be saved for transactionId {TransactionId}",
                        context.Transaction.TransactionId);

                    return new CommitWalletResult
                    {
                        Success = false,
                        Message = "Bank compensation failed and retry state could not be saved. Manual review required."
                    };
                }

                return new CommitWalletResult
                {
                    Success = false,
                    Message = "Bank compensation failed. Retry scheduled."
                };
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,
                    "Compensation call failed for transactionId {TransactionId}",
                    context.Transaction.TransactionId);

                context.Transaction.MarkCompensationRetryFailed(
                    "Bank compensation failed due to service error");

                try
                {
                    await _unitOfWork.SaveChangesAsync(ct);
                }
                catch (Exception saveEx)
                {
                    _logger.LogCritical(saveEx,
                        "Unable to save compensation retry failure for transactionId {TransactionId}",
                        context.Transaction.TransactionId);

                    return new CommitWalletResult
                    {
                        Success = false,
                        Message = "Compensation failed and retry state could not be saved. Manual review required."
                    };
                }

                return new CommitWalletResult
                {
                    Success = false,
                    Message = "Bank compensation failed. Retry scheduled."
                };
            }
        }



        private static string GenerateReferenceId()
        {
            return $"TXN{DateTime.UtcNow:yyyyMMddHHmmssfff}{Random.Shared.Next(100000, 999999)}";
        }
    }
}
