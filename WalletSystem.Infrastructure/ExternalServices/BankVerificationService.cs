using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using WalletSystem.Core.DTOs.Bank;
using WalletSystem.Core.Interfaces.Services;

namespace WalletSystem.Infrastructure.ExternalServices
{
    public class BankVerificationService : IBankVerificationService
    {
        private readonly HttpClient _client;
        private readonly ILogger<BankVerificationService> _logger;

        public BankVerificationService(HttpClient client, ILogger<BankVerificationService> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<BankOperationResponse> CreditAsync(Guid externalBankAccountId, decimal amount, Guid transactionId, CancellationToken ct = default)
        {
            try
            {
                if (externalBankAccountId == Guid.Empty || amount <= 0)
                {
                    return new BankOperationResponse
                    {
                        Success = false,
                        Message = "Invalid Input"
                    };
                }

                var request = new BankOperationRequest
                {
                    ExternalBankAccountId = externalBankAccountId,
                    Amount = amount,
                    ExternalReferenceId = transactionId,
                };

                var response = await _client.PostAsJsonAsync("api/v1/bank/accounts/credit", request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var bankError = await response.Content.ReadFromJsonAsync<BankOperationResponse>(cancellationToken: ct);

                    return bankError ?? new BankOperationResponse
                    {
                        Success = false,
                        Message = "Credit failed"
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<BankOperationResponse>();

                return result ?? new BankOperationResponse
                {
                    Success = false,
                    Message = "Empty response",


                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Credit request failed");

                return new BankOperationResponse
                {
                    Success = false,
                    Message = "Network error"
                };
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "Credit request timeout");

                return new BankOperationResponse
                {
                    Success = false,
                    Message = "Request timeout"
                };
            }
        }

        public async Task<BankOperationResponse> DebitAsync(Guid externalBankAccountId, decimal amount, Guid transactionId, CancellationToken ct = default)
        {

            try
            {
                if (externalBankAccountId == Guid.Empty || amount <= 0)
                {
                    return new BankOperationResponse
                    {
                        Success = false,
                        Message = "Invalid Input"
                    };
                }

                var request = new BankOperationRequest
                {
                    ExternalBankAccountId = externalBankAccountId,
                    Amount = amount,
                    ExternalReferenceId = transactionId,
                };

                _logger.LogInformation(
   "Calling bank debit. Account={AccountId}, Amount={Amount}, Ref={Ref}",
   externalBankAccountId,
   amount,
   transactionId);

                var response = await _client.PostAsJsonAsync("api/v1/bank/accounts/debit", request, ct);

                _logger.LogInformation("Bank debit response status={StatusCode}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    var bankError = await response.Content.ReadFromJsonAsync<BankOperationResponse>(cancellationToken: ct);

                    return bankError ?? new BankOperationResponse
                    {
                        Success = false,
                        Message = "Debit failed"
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<BankOperationResponse>(cancellationToken: ct);

                return result ?? new BankOperationResponse
                {
                    Success = false,
                    Message = "Empty response"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Debit request failed");

                return new BankOperationResponse
                {
                    Success = false,
                    Message = "Network error"
                };
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "Debit request timeout");

                return new BankOperationResponse
                {
                    Success = false,
                    Message = "Request timeout"
                };
            }
        }

        public async Task<CheckBalanceResponse> GetBalanceAsync(Guid externalBankAccountId, CancellationToken ct = default)
        {
            try
            {
                if (externalBankAccountId == Guid.Empty)
                {
                    return new CheckBalanceResponse
                    {
                        Success = false,
                        Message = "Invalid Input"
                    };
                }

                var response = await _client.GetAsync(
                    $"api/v1/bank/accounts/check-balance?externalBankAccountId={externalBankAccountId}", ct
                );

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();

                    return new CheckBalanceResponse
                    {
                        Success = false,
                        Message = string.IsNullOrWhiteSpace(error) ? "Balance fetch failed" : error
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<CheckBalanceResponse>();

                return result ?? new CheckBalanceResponse
                {
                    Success = false,
                    Message = "Empty response"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Balance request failed");

                return new CheckBalanceResponse
                {
                    Success = false,
                    Message = "Network error"
                };
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "Balance request timeout");

                return new CheckBalanceResponse
                {
                    Success = false,
                    Message = "Request timeout"
                };
            }
        }

        public async Task<LinkAccountResult> LinkAsync(string verificationToken, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(verificationToken))
                {
                    return new LinkAccountResult
                    {
                        Success = false,
                        Message = "Invalid Token"
                    };
                }

                var request = new { VerificationToken = verificationToken };

                var response = await _client.PostAsJsonAsync("api/v1/bank/accounts/link", request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();

                    return new LinkAccountResult
                    {
                        Success = false,
                        Message = string.IsNullOrWhiteSpace(error) ? "Link failed" : error
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<LinkAccountResult>();

                return result ?? new LinkAccountResult
                {
                    Success = false,
                    Message = "Empty response"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Link request failed");

                return new LinkAccountResult
                {
                    Success = false,
                    Message = "Network error"
                };
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "Link request timeout");

                return new LinkAccountResult
                {
                    Success = false,
                    Message = "Request timeout"
                };
            }
        }

        public async Task<VerifyBankResponse> VerifyAccountAsync(VerifyBankRequest request, CancellationToken ct = default)
        {
            try
            {
                if (request == null ||
                    string.IsNullOrWhiteSpace(request.AccountNumber) ||
                    string.IsNullOrWhiteSpace(request.AccountHolderName) ||
                    string.IsNullOrWhiteSpace(request.IFSCCode))
                {
                    return new VerifyBankResponse
                    {
                        Success = false,
                        Message = "Invalid Input"
                    };
                }


                var response = await _client.PostAsJsonAsync( "api/v1/bank/accounts/verify", request,
                          BankJsonOptions, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();

                    return new VerifyBankResponse
                    {
                        Success = false,
                        Message = string.IsNullOrWhiteSpace(error) ? "Verification failed" : error
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<VerifyBankResponse>(BankJsonOptions,ct);

                return result ?? new VerifyBankResponse
                {
                    Success = false,
                    Message = "Empty response"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Verify request failed");

                return new VerifyBankResponse
                {
                    Success = false,
                    Message = "Network error"
                };
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "Verify request timeout");

                return new VerifyBankResponse
                {
                    Success = false,
                    Message = "Request timeout"
                };
            }
        }


        private static readonly JsonSerializerOptions BankJsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters =
             {
                 new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
              }
        };


    }
}