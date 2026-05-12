
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WalletSystem.Core.common;
using WalletSystem.Core.DTOs.Bank;
using WalletSystem.Core.Entities;
using WalletSystem.Core.Interfaces.Repositories;
using WalletSystem.Core.Interfaces.Services;

namespace WalletSystem.Services.LinkedBank
{
    public class LinkedBankAccountService : ILinkedBankAccountService
    {

        private readonly ILinkedBankAccountRepository _linkedBankAccountRepository;
        private readonly IBankVerificationService _bankVerificationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LinkedBankAccountService> _logger;
        public LinkedBankAccountService(
            ILinkedBankAccountRepository linkedBankAccountRepository,
            IUnitOfWork unitOfWork,
            IBankVerificationService bankVerificationService,
            ILogger<LinkedBankAccountService> logger
            )
        {
            _linkedBankAccountRepository = linkedBankAccountRepository;
            _bankVerificationService = bankVerificationService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ServiceResult<CheckBalanceResponse>> GetBankBalanceAsync(Guid userId, CancellationToken ct = default)
        {
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("GetLinkedAccountAsync called with empty userId");
                return ServiceResult<CheckBalanceResponse>.Fail("Empty Input");
            }

            _logger.LogInformation("Fetching linked bank account  for UserId: {UserId}", userId);

            var userBankAccount = await _linkedBankAccountRepository.GetByUserIdAsync(userId, ct);

            if (userBankAccount == null)
            {
                return ServiceResult<CheckBalanceResponse>.Fail("Bank Account Not Found");
            }

            if (userBankAccount.ExternalBankAccountId == Guid.Empty)
            {
                return ServiceResult<CheckBalanceResponse>.Fail("Invalid linked account");
            }

            var getBalance = await _bankVerificationService
                .GetBalanceAsync(userBankAccount.ExternalBankAccountId, ct);

            if (!getBalance.Success)
            {
                return ServiceResult<CheckBalanceResponse>.Fail(getBalance.Message ?? "Unable to Fetch Balance");
            }

            return ServiceResult<CheckBalanceResponse>.Ok(new CheckBalanceResponse
            {
                 Success = true,
                Balance = getBalance.Balance,
                Message  = getBalance.Message ?? "Balance Fetched Successfully"           
            });
        }

        public async Task<ServiceResult<LinkedBankAccountResponse>> GetLinkedAccountAsync(Guid userId, CancellationToken ct = default)
        {
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("GetLinkedAccountAsync called with empty userId");
                return ServiceResult<LinkedBankAccountResponse>.Fail("Empty Input");
            }

            _logger.LogInformation("Fetching linked bank account for UserId: {UserId}", userId);

            var userBankAccount = await _linkedBankAccountRepository.GetByUserIdAsync(userId, ct);

            if (userBankAccount == null)
            {
                _logger.LogInformation("No linked bank account found for UserId: {UserId}", userId);
                return ServiceResult<LinkedBankAccountResponse>.Fail("Bank Account Not Found");
            }

            if (userBankAccount.ExternalBankAccountId == Guid.Empty)
            {
                _logger.LogWarning(
                    "Linked account exists but ExternalBankAccountId is empty for UserId: {UserId}",
                    userId);

                return ServiceResult<LinkedBankAccountResponse>.Fail("Invalid linked account");
            }

            return ServiceResult<LinkedBankAccountResponse>.Ok(new LinkedBankAccountResponse
            {
                ExternalReferenceId = userBankAccount.ExternalBankAccountId,
                MaskedAccountNumber = userBankAccount.MaskedAccountNumber,
                AccountHolderName = userBankAccount.AccountHolderName,
                BankName = userBankAccount.BankName,
                IFSCCode = userBankAccount.IFSCCode
            });
        }

        public async Task<ServiceResult<LinkedBankResponse>> VerifyAndLinkBankAccountAsync(
               Guid userId,
               VerifyBankRequest request
            , CancellationToken ct = default)
        {
            _logger.LogInformation("Inside VerifyAndLinkBankAccountAsync");
            if (userId == Guid.Empty || request == null)
            {
                return ServiceResult<LinkedBankResponse>.Fail("Empty Input");
            }

            if (string.IsNullOrWhiteSpace(request.AccountHolderName) ||
                string.IsNullOrWhiteSpace(request.IFSCCode) ||
                string.IsNullOrWhiteSpace(request.AccountNumber)
                )
            {
                return ServiceResult<LinkedBankResponse>.Fail("Empty Input");
            }

            if (request.AccountType != Core.Enums.AccountType.Savings)
            {
                return ServiceResult<LinkedBankResponse>.Fail("Wallet supports Savings account only");
            }

            var checkExists = await _linkedBankAccountRepository.ExistsByUserIdAsync(userId, ct);
            if (checkExists)
            {
                return ServiceResult<LinkedBankResponse>.Fail("Bank account already linked");
            }

            _logger.LogInformation("Calling bank verify API");
            var verifyApiResponse = await _bankVerificationService.VerifyAccountAsync(request, ct);
            _logger.LogInformation("Verify response: {@Response}", verifyApiResponse);

            if (!verifyApiResponse.Success)
            {
                _logger.LogWarning("Bank verification failed for user {UserId}: {Message}",
                    userId, verifyApiResponse.Message);

                return ServiceResult<LinkedBankResponse>.Fail(
                    verifyApiResponse.Message ?? "Verification failed");
            }
    
            _logger.LogInformation(verifyApiResponse.Message);

            if (string.IsNullOrWhiteSpace(verifyApiResponse.VerificationToken))
            {
                return ServiceResult<LinkedBankResponse>.Fail("Verification token missing");
            }

            if (string.IsNullOrWhiteSpace(verifyApiResponse.IFSCCode) ||
               string.IsNullOrWhiteSpace(verifyApiResponse.MaskedAccountNumber) ||
               string.IsNullOrWhiteSpace(verifyApiResponse.BankName) ||
               string.IsNullOrWhiteSpace(verifyApiResponse.AccountHolderName))
            {
                _logger.LogError("Invalid verification response: missing required fields");
                return ServiceResult<LinkedBankResponse>.Fail("Invalid bank verification response");
            }



            var linkApiResponse = await _bankVerificationService
                .LinkAsync(verifyApiResponse.VerificationToken, ct);

            _logger.LogInformation("external id information {id}", linkApiResponse.ExternalReferenceId);

            if (!linkApiResponse.Success || !linkApiResponse.ExternalReferenceId.HasValue)
            {
                _logger.LogWarning("Bank linking failed for user {UserId}: {Message}",
                    userId, linkApiResponse.Message);

                return ServiceResult<LinkedBankResponse>.Fail(
                    linkApiResponse.Message ?? "Linking failed");
            }


            var linkedBankAccount = new LinkedBankAccount
            {
                LinkedBankAccountId = Guid.NewGuid(),
                UserId = userId,
              
                IFSCCode = verifyApiResponse.IFSCCode,
                ExternalBankAccountId = linkApiResponse.ExternalReferenceId.Value,
                MaskedAccountNumber = verifyApiResponse.MaskedAccountNumber,
                BankName = verifyApiResponse.BankName,
                AccountHolderName = verifyApiResponse.AccountHolderName,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };


         

            await _linkedBankAccountRepository.AddAsync(linkedBankAccount, ct);

            try
            {
                await _unitOfWork.SaveChangesAsync(ct);

                _logger.LogInformation("Bank linked successfully for user {UserId}", userId);

                return ServiceResult<LinkedBankResponse>.Ok(new LinkedBankResponse
                {
                    Success = true,
                    Message = "Bank linked successfully",

                    ExternalReferenceId = linkApiResponse.ExternalReferenceId.Value,
                     IFSCCode = verifyApiResponse.IFSCCode,
                    MaskedAccountNumber = verifyApiResponse.MaskedAccountNumber,
                    AccountHolderName = verifyApiResponse.AccountHolderName,
                    AccountType = verifyApiResponse.AccountType,
                    BankName = verifyApiResponse.BankName
                    
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save linked bank account for user {UserId}", userId);

                return ServiceResult<LinkedBankResponse>.Fail(
                    "Unable to save linked bank account");
            }
        }
    }
}
