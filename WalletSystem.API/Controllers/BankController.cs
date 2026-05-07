using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.API.Models;
using WalletSystem.Core.DTOs.Bank;
using WalletSystem.Core.Interfaces.Services;

namespace WalletSystem.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class BankController : BaseController
    {
        private readonly ILinkedBankAccountService _linkedBankAccountService;
        private readonly ILogger<BankController> _logger;
        public BankController(ILinkedBankAccountService linkedBankAccountService, ILogger<BankController> logger)
        {
            _linkedBankAccountService = linkedBankAccountService;
            _logger = logger;
        }


        [HttpGet("balance")]
        public async Task<IActionResult> GetBankBalance(CancellationToken ct = default)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid User"));
            }

            _logger.LogInformation("Fetching bank balance for user {UserId}", userId);

            var result = await _linkedBankAccountService.GetBankBalanceAsync(userId, ct);



            if (!result.Success)
            {
                return NotFound(ApiResponse<object>.Fail(result.Message ?? "Failed to fetch balance"));
            }
            var res = result.Result!;
            return Ok(ApiResponse<CheckBalanceResponse>.Ok(res, "Success"));
        }



        [HttpGet("account")]
        public async Task<IActionResult> GetAccountDetails(CancellationToken ct = default)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid User"));
            }

            _logger.LogInformation("Fetching bank account for user {UserId}", userId);
            var result = await _linkedBankAccountService.GetLinkedAccountAsync(userId, ct);


            if (!result.Success)
            {
                return NotFound(ApiResponse<object>.Fail(result.Message ?? "Api Issue for Getting Linked Account"));
            }

            var res = result.Result!;
            return Ok(ApiResponse<LinkedBankAccountResponse>.Ok(res, "Success"));

        }


        [HttpPost("link")]
        public async Task<IActionResult> VerifyAndLinkAccount([FromBody] VerifyBankRequest verifyBankRequest, CancellationToken ct = default)
        {
            _logger.LogInformation("Reached controller");
            _logger.LogInformation("ModelState Valid: {Valid}", ModelState.IsValid);
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                   .SelectMany(v => v.Errors)
                   .Select(e => e.ErrorMessage);
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogError("Validation Error: {Error}", error.ErrorMessage);
                }
                return BadRequest(ApiResponse<object>.Fail(string.Join(", ", errors)));
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid User"));
            }


            _logger.LogInformation("Verifyin and linking  bank account  for user {UserId}", userId);
            var result = await _linkedBankAccountService.VerifyAndLinkBankAccountAsync(userId, verifyBankRequest, ct);


            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.Fail(result.Message ?? "Api Issue for Linking Account"));
            }

            var res = result.Result!;
            return Ok(ApiResponse<LinkedBankReponse>.Ok(res, "Success"));

        }
    }
}
