
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WalletSystem.Core.Entities;
using WalletSystem.Core.Enums;
using WalletSystem.Core.Interfaces.Services;
using WalletSystem.Infrastructure.Data;

namespace WalletSystem.Services.Background
{
    public class CompensationRecoveryService : BackgroundService
    {

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CompensationRecoveryService> _logger;

        private const int BatchSize = 50;
        private const int MaxRetries = 3;

        public CompensationRecoveryService(IServiceScopeFactory scopeFactory, ILogger<CompensationRecoveryService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

                try
                {
                    await ProcessPendingCompensationsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Compensation recovery cycle failed");
                }
            }
        }

        private async Task ProcessPendingCompensationsAsync(CancellationToken ct)
        {

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WalletContext>();

            var bankService = scope.ServiceProvider.GetRequiredService<IBankVerificationService>();

            var now = DateTime.UtcNow;

            //fetching data in batches

            var pending = await context.Transactions.Where(t =>
                           (t.Status == TransactionStatus.CompensationPending ||
                            t.Status == TransactionStatus.CompensationRetrying) &&
                            t.NextRetryAt != null && t.NextRetryAt <= now && t.RetryCount < MaxRetries)
                .OrderBy(t => t.NextRetryAt).Take(BatchSize).ToListAsync(ct);


            if (!pending.Any())
            {
                return;
            }
            _logger.LogInformation("Processing {Count} compensation(s)", pending.Count);

            foreach(var tx in pending)
            {
                await CompensateSingleAsync(tx, bankService, context, ct);
                await context.SaveChangesAsync(ct);
            }
           
        }


        private async Task CompensateSingleAsync(Transaction tx, IBankVerificationService bankService, WalletContext context, CancellationToken ct)
        {
            try
            {
                if (tx.SourceBankAccountId == null)
                {
                    tx.MarkCompensationRetryFailed("No bank account ID on transaction");
                    return;
                }

                var result = await bankService.CreditAsync(
                            tx.SourceBankAccountId.Value,
                            tx.Amount,
                            tx.TransactionId,
                            ct

                    );
                if (result.Success || result.IsIdempotentReplay)
                {
                    tx.MarkCompensated();
                    _logger.LogInformation(
                   "Compensation succeeded for {Id} (replay={Replay})", tx.TransactionId, result.IsIdempotentReplay);
                }
                else
                {
                    tx.MarkCompensationRetryFailed(result.Message ?? "Bank credit failed");

                    if (tx.Status == TransactionStatus.ManualReviewRequired)
                    {
                        _logger.LogCritical(
                            "Transaction {Id} requires manual review after {Max} retries", tx.TransactionId, MaxRetries);
                    }
                }


            }
            catch (Exception ex)
            {
                tx.MarkCompensationRetryFailed(ex.Message);
                _logger.LogError(ex, "Compensation attempt failed for {Id}", tx.TransactionId);
            }
        }


    }
}
