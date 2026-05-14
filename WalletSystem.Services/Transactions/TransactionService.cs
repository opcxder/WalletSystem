
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data.Common;
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
        private readonly IUserCredentialsRepository _userCredentialsRepository;
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
                                  IUserCredentialsRepository userCredentialsRepository,
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
            _userCredentialsRepository = userCredentialsRepository;
            _vpaRepository = vpaRepository;
            _walletContext = walletContext;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }


        public async Task<ServiceResult<TransactionResponse>> AddMoneyAsync(Guid userId, AddMoneyRequest request, CancellationToken ct = default)
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
                       "Transaction Already exists with different amount.");
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
                return ServiceResult<List<TransactionResponse>>.Ok(new());
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

        public async Task<ServiceResult<TransactionResponse>> SendMoneyAsync(Guid userId, SendMoneyRequest request, CancellationToken ct = default)
        {
            try
            {
                var validateAndPrepaereResult = await ValidateAndPrepareSendMoneyAsync(userId, request, ct);
                if (!validateAndPrepaereResult.Success)
                {
                    return ServiceResult<TransactionResponse>.Fail(validateAndPrepaereResult.Message ?? "Invalid Input");
                }

                if (validateAndPrepaereResult.TransactionResponse != null)
                {
                    return ServiceResult<TransactionResponse>.Ok(validateAndPrepaereResult.TransactionResponse);
                }

                var context = validateAndPrepaereResult.SendMoneyContext;

                if (context == null)
                {
                    return ServiceResult<TransactionResponse>.Fail(validateAndPrepaereResult.Message ?? "Validate Input Error");
                }

                try
                {
                    await _transactionRepository.AddAsync(context.Transaction, ct);
                    await _unitOfWork.SaveChangesAsync(ct);

                }
                catch (DbUpdateException ex)
                {
                    _logger.LogWarning(ex, "Send money transaction insert failed. Checking for idempotent replay.");

                    var existingTransaction = await _transactionRepository.GetSendMoneyByIdempotencyKeyAsync(
                          context.SenderWallet.WalletId,
                          context.ReceiverWallet.WalletId,
                           context.Transaction.IdempotencyKey, ct
                        );

                    if (existingTransaction != null && existingTransaction.Amount == request.Amount)
                    {
                        return ServiceResult<TransactionResponse>.Ok(new TransactionResponse
                        {
                            Amount = existingTransaction.Amount,
                            TransactionId = existingTransaction.TransactionId,
                            Status = existingTransaction.Status,
                            CreatedAt = existingTransaction.CreatedAt,
                            IsIdempotentReplay = true,
                            ReferenceId = existingTransaction.ReferenceId ?? "Not Found",
                            FailureReason = existingTransaction.FailureReason ?? existingTransaction.CompensationFailureReason,
                            Type = existingTransaction.Type
                        });
                    }
                    if (existingTransaction != null && existingTransaction.Amount != request.Amount)
                    {
                        return ServiceResult<TransactionResponse>.Fail("Transaction Already exists with different amount");
                    }

                    if (existingTransaction == null)
                    {
                        return ServiceResult<TransactionResponse>.Fail("Unable to create transaction, Please try again.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create send money transaction.");
                    return ServiceResult<TransactionResponse>.Fail("Unable to create the transaction, Please try again");
                }
                int maxRetries = 3;

                for (int attempt = 0; attempt < maxRetries; attempt++)
                {
                    try
                    {
                        var transferResult = await CommitTransferAsync(context, ct);

                        // business failure (non-exception)
                        if (!transferResult.Success)
                        {
                            context.Transaction.MarkFailed(
                                transferResult.Message ?? "Transfer failed");

                            await _unitOfWork.SaveChangesAsync(ct);

                            return ServiceResult<TransactionResponse>.Fail(
                                transferResult.Message ?? "Transfer failed");
                        }

                        // success
                        return ServiceResult<TransactionResponse>.Ok(
                            new TransactionResponse
                            {
                                TransactionId = context.Transaction.TransactionId,
                                Amount = context.Transaction.Amount,
                                Status = context.Transaction.Status,
                                IsIdempotentReplay = false,
                                CreatedAt = context.Transaction.CreatedAt,
                                Type = context.Transaction.Type,
                                ReferenceId = context.Transaction.ReferenceId ?? "Not found"
                            });
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Concurrency conflict on attempt {Attempt} for transaction {TransactionId}",
                            attempt + 1,
                            context.Transaction.TransactionId);

                        // final retry exhausted
                        if (attempt == maxRetries - 1)
                        {
                            context.Transaction.MarkFailed(
                                "Too many concurrent requests");

                            await _unitOfWork.SaveChangesAsync(ct);

                            return ServiceResult<TransactionResponse>.Fail(
                                "Too many concurrent requests. Please retry.");
                        }


                        // retry next loop iteration
                        continue;
                    }
                    catch (ArgumentException ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Invalid transfer amount for transaction {TransactionId}",
                            context.Transaction.TransactionId);

                        context.Transaction.MarkFailed(ex.Message);

                        await _unitOfWork.SaveChangesAsync(ct);

                        return ServiceResult<TransactionResponse>.Fail(
                            "Transfer amount must be greater than zero.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Business rule failure for transaction {TransactionId}",
                            context.Transaction.TransactionId);

                        context.Transaction.MarkFailed(ex.Message);

                        await _unitOfWork.SaveChangesAsync(ct);

                        return ServiceResult<TransactionResponse>.Fail(
                            ex.Message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Unexpected error while processing transaction {TransactionId}",
                            context.Transaction.TransactionId);

                        context.Transaction.MarkFailed(
                            "Unexpected transfer error");

                        await _unitOfWork.SaveChangesAsync(ct);

                        return ServiceResult<TransactionResponse>.Fail(
                            "Something went wrong while transferring money.");
                    }
                }

                // defensive fallback
                return ServiceResult<TransactionResponse>.Fail(
                    "Transfer could not be completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in Send Money");

                return ServiceResult<TransactionResponse>.Fail("Unexpected error");
            }
        }

        private async Task<AddMoneyValidationResult> ValidateAndPrepareAsync(Guid userId, AddMoneyRequest request, CancellationToken ct)
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

        private async Task<BankDebitResult> DebitBankAsync(AddMoneyContext context, CancellationToken ct)
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

        private async Task<CommitWalletResult> CommitWalletCreditAsync(AddMoneyContext context, CancellationToken ct, int attempt = 0)
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
        private async Task<CommitWalletResult> CompensateBankCreditAsync(AddMoneyContext context, CancellationToken ct)
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


        private async Task<SendMoneyValidationResult> ValidateAndPrepareSendMoneyAsync(Guid userId, SendMoneyRequest request, CancellationToken ct)
        {
            if (userId == Guid.Empty)
            {
                return new SendMoneyValidationResult
                {
                    Success = false,
                    Message = "Empty Input"
                };
            }

            if (request == null)
            {
                return new SendMoneyValidationResult
                {
                    Success = false,
                    Message = "Empty Request"
                };
            }

            if (request.Amount <= 0 || request.Amount > 10000)
            {
                return new SendMoneyValidationResult
                {
                    Success = false,
                    Message = "Invalid amount"
                };
            }

            if (string.IsNullOrWhiteSpace(request.IdempotencyKey) ||
                string.IsNullOrWhiteSpace(request.ReceiverVpa) ||
                string.IsNullOrWhiteSpace(request.PaymentPin)
                )
            {
                return new SendMoneyValidationResult
                {
                    Success = false,
                    Message = "Request parameter empty"
                };
            }


            var normalizedKey = request.IdempotencyKey.Trim().ToLowerInvariant();
            var normalizedReceiverVpa = request.ReceiverVpa.Trim().ToLowerInvariant();

            var senderUser = await _userRepository.GetByIdAsync(userId, ct);
            if (senderUser == null)
            {
                return new SendMoneyValidationResult
                {
                    Success = false,
                    Message = "Sender User not found"
                };
            }

            if (senderUser.Status != UserStatus.Active)
            {
                return new SendMoneyValidationResult
                {
                    Success = false,
                    Message = "Sender User not Active"
                };
            }

            var senderWallet = await _walletRepository.GetByUserIdForUpdateAsync(userId, ct);
            if (senderWallet == null)
            {
                return new SendMoneyValidationResult
                {
                    Success = false,
                    Message = "Sender wallet not found"
                };
            }

            if (senderWallet.Status != WalletStatus.Active)
            {
                return new SendMoneyValidationResult
                {
                    Success = false,
                    Message = "Sender Wallet not Active"
                };
            }

            var receiverVpa = await _vpaRepository.GetByAddressAsync(normalizedReceiverVpa, ct);
            if (receiverVpa == null)
            {
                return new SendMoneyValidationResult
                {
                    Success = false,
                    Message = "Receier Vpa Address not found"
                };
            }

            if (senderWallet.WalletId == receiverVpa.WalletId)
            {
                return new SendMoneyValidationResult
                {
                    Success = false,
                    Message = "Cannot transfer to same wallet"
                };
            }

            var receiverWallet = await _walletRepository.GetByWalletIdForUpdateAsync(receiverVpa.WalletId, ct);
            if (receiverWallet == null)
            {
                return new SendMoneyValidationResult
                {
                    Success = false,
                    Message = " Receiver wallet not found"
                };
            }
            if (receiverWallet.Status != WalletStatus.Active)
            {
                return new SendMoneyValidationResult
                {
                    Success = false,
                    Message = " Recevier Wallet is not active"
                };
            }

            var receiverUser = await _userRepository.GetByIdAsync(receiverWallet.UserId, ct);
            if (receiverUser == null)
            {
                return new SendMoneyValidationResult
                {
                    Success = false,
                    Message = "Receiver User not found"
                };
            }


            if (receiverUser.Status != UserStatus.Active)
            {
                return new SendMoneyValidationResult
                {
                    Success = false,
                    Message = " Receiver User not Active"
                };
            }


            var userCredentials = await _userCredentialsRepository.GetByUserIdAsync(userId, ct);
            if (userCredentials == null)
            {
                return new SendMoneyValidationResult
                {
                    Success = false,
                    Message = " User  credentails  not found"
                };
            }

            if (!BCrypt.Net.BCrypt.Verify(request.PaymentPin, userCredentials.PaymentPinHash))
            {
                return new SendMoneyValidationResult
                {
                    Success = false,
                    Message = "Invalid payment PIN"
                };
            }


            if (senderWallet.Balance < request.Amount)
            {
                return new SendMoneyValidationResult
                {
                    Success = false,
                    Message = "Insufficient Balance"
                };
            }

            var existingTransaction = await _transactionRepository.GetSendMoneyByIdempotencyKeyAsync(senderWallet.WalletId, receiverWallet.WalletId, normalizedKey, ct);
            if (existingTransaction != null)
            {

                if (existingTransaction.Amount == request.Amount)
                {
                    return new SendMoneyValidationResult
                    {
                        Success = true,
                        Message = "Transaction found with same ID",
                        TransactionResponse = new TransactionResponse
                        {
                            TransactionId = existingTransaction.TransactionId,
                            ReferenceId = existingTransaction.ReferenceId ?? "Not found",
                            Status = existingTransaction.Status,
                            Amount = existingTransaction.Amount,
                            CreatedAt = existingTransaction.CreatedAt,
                            IsIdempotentReplay = true,
                            Type = existingTransaction.Type,
                            FailureReason = existingTransaction.FailureReason ?? existingTransaction.CompensationFailureReason
                        }

                    };
                }

                return new SendMoneyValidationResult
                {
                    Success = false,
                    Message = "Transaction already exists with different amount"
                };
            }


            var transaction = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                SourceType = SourceType.Wallet,
                SourceWalletId = senderWallet.WalletId,
                ReferenceId = GenerateReferenceId(),
                DestinationWalletId = receiverWallet.WalletId,
                DestinationVPA = receiverVpa.VpaAddress,
                Amount = request.Amount,
                Status = TransactionStatus.Initiated,
                IdempotencyKey = normalizedKey,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Description = "Wallet Transfer Initiated",
                Type = TransactionType.Transfer
            };


            return new SendMoneyValidationResult
            {
                Success = true,
                Message = "Input Validation done",
                SendMoneyContext = new SendMoneyContext
                {
                    SenderUser = senderUser,
                    SenderWallet = senderWallet,
                    ReceiverVpa = receiverVpa,
                    ReceiverWallet = receiverWallet,
                    Transaction = transaction,
                    ReceiverUser = receiverUser
                }
            };

        }



        private async Task<CommitTransferResult> CommitTransferAsync(SendMoneyContext context, CancellationToken ct)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {

                var senderWallet = await _walletRepository.GetByUserIdForUpdateAsync(context.SenderUser.UserId,ct);

                if (senderWallet == null)
                {
                    return new CommitTransferResult
                    {   
                        Success = false,
                        Message = " Sender Wallet Not Found",
                        Retryable = false,
                    };
                }

                if (senderWallet.Status != WalletStatus.Active)
                {
                    return new CommitTransferResult
                    {
                        Success = false,
                        Retryable = false,
                        Message = "Sender Wallet is not Active",
                    };
                }

                if (context.SenderUser.Status != UserStatus.Active)
                {
                    return new CommitTransferResult
                    {
                        Success = false,
                        Retryable = false,
                        Message = "User is not active"
                    };
                }

                var receiverWallet = await _walletRepository.GetByWalletIdForUpdateAsync(context.ReceiverWallet.WalletId,ct);

                if (receiverWallet == null)
                {
                    return new CommitTransferResult
                    {
                        Success = false,
                        Message = " Receiver Wallet Not Found",
                        Retryable = false,
                    };
                }

                if (receiverWallet.Status != WalletStatus.Active)
                {
                    return new CommitTransferResult
                    {
                        Success = false,
                        Retryable = false,
                        Message = "Receiver Wallet is not Active",
                    };
                }

                if (context.ReceiverUser.Status != UserStatus.Active)
                {
                    return new CommitTransferResult
                    {
                        Success = false,
                        Retryable = false,
                        Message = "Receiver User is not active"
                    };
                }

                if (context.Transaction.Amount > senderWallet.Balance)
                {
                    return new CommitTransferResult
                    {
                        Success = false,
                        Message = "Insufficient Balance",
                        Retryable = false,
                    };
                }

                var balanceBefore = senderWallet.Balance;
                senderWallet.ApplyDebit(context.Transaction.Amount);
                var balanceAfter = senderWallet.Balance;


                await _walletContext.LedgerEntries.AddAsync(new LedgerEntry
                {
                    EntryId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    BalanceBefore = balanceBefore,
                    Amount = context.Transaction.Amount,
                    BalanceAfter = balanceAfter,
                    EntryType = EntryType.Debit,
                    TransactionId = context.Transaction.TransactionId,
                    WalletId = senderWallet.WalletId,

                });


                var receiverBalanceBefore = receiverWallet.Balance;
                receiverWallet.ApplyCredit(context.Transaction.Amount);
                var receiverBalanceAfter = receiverWallet.Balance;

                await _walletContext.LedgerEntries.AddAsync(new LedgerEntry
                {
                    EntryId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    BalanceBefore = receiverBalanceBefore,
                    Amount = context.Transaction.Amount,
                    BalanceAfter = receiverBalanceAfter,
                    EntryType = EntryType.Credit,
                    TransactionId = context.Transaction.TransactionId,
                    WalletId = receiverWallet.WalletId,

                });


                context.Transaction.MarkSuccess();
                return new CommitTransferResult
                {
                    Success = true,
                    Retryable = false,
                    Message = "Transaction Successfull"
                };
            });
        }





        private static string GenerateReferenceId()
        {
            return $"TXN{DateTime.UtcNow:yyyyMMddHHmmssfff}{Random.Shared.Next(100000, 999999)}";
        }


    }
}
