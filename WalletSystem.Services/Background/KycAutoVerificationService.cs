using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WalletSystem.Core.Enums;
using WalletSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace WalletSystem.Services.Background
{
    public class KycAutoVerificationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<KycAutoVerificationService> _logger;

        public KycAutoVerificationService(
            IServiceScopeFactory scopeFactory,
            ILogger<KycAutoVerificationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
                await AutoVerifyPendingKycAsync(stoppingToken);
            }
        }

        private async Task AutoVerifyPendingKycAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WalletContext>();

            var cutoff = DateTime.UtcNow.AddMinutes(-10);

            var pendingKycs = await context.UserKycs
                .Where(k => k.Status == KycStatus.Pending && k.CreatedAt <= cutoff)
                .ToListAsync(ct);

            foreach (var kyc in pendingKycs)
            {
                kyc.Status = KycStatus.Verified;
                kyc.VerifiedAt = DateTime.UtcNow;
                kyc.UpdatedAt = DateTime.UtcNow;
            }

            if (pendingKycs.Any())
            {
                await context.SaveChangesAsync(ct);
                _logger.LogInformation("Auto-verified {Count} KYC records", pendingKycs.Count);
            }
        }
    }
}