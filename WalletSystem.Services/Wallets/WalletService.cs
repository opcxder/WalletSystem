
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using WalletSystem.Core.common;

using WalletSystem.Core.DTOs.Wallets;
using WalletSystem.Core.Entities;
using WalletSystem.Core.Enums;
using WalletSystem.Core.Interfaces.Repositories;
using WalletSystem.Core.Interfaces.Services;

namespace WalletSystem.Services.Wallet
{
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _walletRepository;
        private readonly ILogger<WalletService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserCredentialsRepository _userCredentialsRepository;
        private readonly IUserRepository _userRepository;
        private readonly IVpaRepository _vpaRepository;
        public WalletService(IWalletRepository walletRepository,
                             ILogger<WalletService> logger,
                              IUnitOfWork unitOfWork,
                         IUserCredentialsRepository userCredentialsRepository,
                          IUserRepository userRepository,
                          IVpaRepository vpaRepository
            )
        {
            _walletRepository = walletRepository;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _userCredentialsRepository = userCredentialsRepository;
            _userRepository = userRepository;
            _vpaRepository = vpaRepository;
        }

        public async Task<ServiceResult<WalletResponse>> CreateWalletAsync(
            Guid userId,
            CreateWalletRequest request,
            CancellationToken ct = default)
        {
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("CreateWalletAsync called with empty userId");

                return ServiceResult<WalletResponse>.Fail("Empty input");
            }

            if (request == null || string.IsNullOrWhiteSpace(request.PaymentPin))
            {
                _logger.LogWarning("CreateWalletAsync called with invalid input");

                return ServiceResult<WalletResponse>.Fail("Invalid input");
            }

            if (!Regex.IsMatch(request.PaymentPin, @"^\d{4,6}$"))
            {
                return ServiceResult<WalletResponse>
                    .Fail("PIN must be 4 to 6 digits");
            }

            var user = await _userRepository.GetByIdAsync(userId, ct);
            if (user == null)
                return ServiceResult<WalletResponse>.Fail("User not found");

            if (!user.IsEmailVerified || user.Status != UserStatus.Active)
                return ServiceResult<WalletResponse>.Fail("Account must be verified before creating a wallet");

            var existsWallet = await _walletRepository.ExistsByUserIdAsync(userId, ct);

            if (existsWallet)
            {
                _logger.LogWarning("Wallet already exists for userId {userId}", userId);

                return ServiceResult<WalletResponse>.Fail("Wallet already exists");
            }

            var wallet = new Core.Entities.Wallet
            {
                WalletId = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = Core.Enums.WalletStatus.Active,
            };


             var userCredentials = await _userCredentialsRepository.GetByUserIdForUpdateAsync(userId, ct);
            if (userCredentials == null)
            {
                _logger.LogWarning("User credentials not found for userId {id}: ", userId);
                return ServiceResult<WalletResponse>.Fail("User credentials not found");
            }

            string paymentPinHash = BCrypt.Net.BCrypt.HashPassword(request.PaymentPin);
            userCredentials.PaymentPinHash = paymentPinHash;


            string? userVpa = null;

            for (int i = 0; i < 3; i++)
            {
                userVpa = GenerateVpa(user.FullName);

                if (string.IsNullOrWhiteSpace(userVpa))
                {
                    _logger.LogWarning("Unable to generate VPA for userId {userId}", userId);
                    return ServiceResult<WalletResponse>.Fail("Error generating VPA");
                }

                bool exists = await _vpaRepository.ExistsAsync(userVpa, ct);

                if (!exists)
                {
                    break; 
                }

                _logger.LogInformation("VPA collision found, retrying... attempt {attempt}", i + 1);
            }



            if ( string.IsNullOrWhiteSpace(userVpa) ||  await _vpaRepository.ExistsAsync(userVpa, ct))
            {
                return ServiceResult<WalletResponse>.Fail("Unable to generate unique VPA after retries");
            }

            var vpa = new Vpa
            {
                VpaId = Guid.NewGuid(),
                VpaAddress = userVpa,
                WalletId = wallet.WalletId,
                IsPrimary = true,
                CreatedAt = DateTime.UtcNow,
            };

            try
            {
                await _walletRepository.AddAsync(wallet, ct);
                await _userCredentialsRepository.UpdateAsync(userCredentials);
                await _vpaRepository.AddAsync(vpa, ct);

                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Wallet creation failed for userId {userId}",
                    userId);

                return ServiceResult<WalletResponse>.Fail("Failed to save wallet");
            }

            return ServiceResult<WalletResponse>.Ok(new WalletResponse
            {
                WalletId = wallet.WalletId,
                Status = wallet.Status,
                Balance = wallet.Balance,
                CreatedAt = wallet.CreatedAt,
                VpaAddress = vpa.VpaAddress
            });
        }
        public async Task<ServiceResult<WalletBalanceResponse>> GetBalanceAsync(
            Guid userId,
            CancellationToken ct = default)
        {
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("GetBalanceAsync called with empty userId");

                return ServiceResult<WalletBalanceResponse>.Fail("Empty input");
            }

            _logger.LogInformation("Fetching wallet balance for userId {id}", userId);

            var wallet = await _walletRepository.GetByUserIdAsync(userId, ct);

            if (wallet == null)
            {
                _logger.LogWarning("Wallet not found for userId {id}", userId);

                return ServiceResult<WalletBalanceResponse>.Fail("Wallet not found");
            }

            return ServiceResult<WalletBalanceResponse>.Ok(new WalletBalanceResponse
            {
                Balance = wallet.Balance
            });
        }
        public async Task<ServiceResult<WalletResponse>> GetWalletByUserIdAsync(
            Guid userId,
            CancellationToken ct = default)
        {

            if (userId == Guid.Empty)
            {
                _logger.LogWarning("GetWalletByUserIdAsync called with empty userId");

                return ServiceResult<WalletResponse>.Fail("Empty Input");
            }

            _logger.LogInformation("Fetching user wallet for userId {id}", userId);

            var wallet = await _walletRepository.GetByUserIdAsync(userId, ct);

            if (wallet == null)
            {
                _logger.LogWarning("Wallet not found for userId {id}", userId);

                return ServiceResult<WalletResponse>.Fail("Wallet not found");
            }

            var vpa = await _vpaRepository.GetByWalletIdAsync(wallet.WalletId, ct);




            return ServiceResult<WalletResponse>.Ok(new WalletResponse
            {
                WalletId = wallet.WalletId,
                Balance = wallet.Balance,
                Status = wallet.Status,
                CreatedAt = wallet.CreatedAt,
                VpaAddress = vpa?.VpaAddress ?? "Address not found"

            });
        }


        private static string GenerateVpa(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Full name cannot be empty");

            var parts = fullName
                .Trim()
                .ToLower()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var first = new string(parts[0]
                .Where(char.IsLetter)
                .ToArray());

            string lastInitial = "";

            if (parts.Length > 1)
            {
                var last = new string(parts[^1]
                    .Where(char.IsLetter)
                    .ToArray());

                if (!string.IsNullOrEmpty(last))
                {
                    lastInitial = last.Substring(0, 1);
                }
            }

            var random = Random.Shared.Next(100, 1000);

            return $"{first}.{lastInitial}.{random}@wpay";
        }
    }
}
