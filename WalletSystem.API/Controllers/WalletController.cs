
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.API.Models;

using WalletSystem.Core.DTOs.Wallets;

using WalletSystem.Core.Interfaces.Services;

namespace WalletSystem.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class WalletController : BaseController
    {

        private readonly IWalletService _walletService;
        private readonly ILogger<WalletController> _logger;

        public WalletController(IWalletService walletService, ILogger<WalletController> logger)
        {
            _walletService = walletService;
            _logger = logger;
        }


        [HttpGet("balance")]
        public async Task<IActionResult> GetWalletBalance(CancellationToken ct = default)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid user"));
            }

            var balance = await _walletService.GetBalanceAsync(userId, ct);
            if (!balance.Success)
            {
                return NotFound(ApiResponse<object>.Fail(balance.Message ?? "Failed to fetch wallet"));
            }

            if (balance.Result == null)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Unexpected null result"));
            }
            return Ok(ApiResponse<WalletBalanceResponse>.Ok(balance.Result, "Success"));
        }


        [HttpGet]
        public async Task<IActionResult> GetWallet(CancellationToken ct = default)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid User"));
            }

            var wallet = await _walletService.GetWalletByUserIdAsync(userId, ct);

            if (!wallet.Success)
            {
                return NotFound(ApiResponse<object>.Fail(wallet.Message ?? "Failed to fetch wallet"));
            }

            if (wallet.Result == null)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Unexpected null result"));
            }
            return Ok(ApiResponse<WalletResponse>.Ok(wallet.Result, "Success"));
        }


        [HttpPost]
        public async Task<IActionResult> CreateWallet( [FromBody] CreateWalletRequest request, CancellationToken ct = default)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid user"));
            }

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


            var wallet = await _walletService.CreateWalletAsync(userId, request, ct);

            if (!wallet.Success)
            {
                return BadRequest(ApiResponse<object>.Fail(wallet.Message ?? "Api Issue for Creating Wallet"));
            }

            if (wallet.Result == null)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Unexpected null result"));
            }
            return StatusCode(201, ApiResponse<WalletResponse>.Ok(wallet.Result, "Wallet created successfully"));
        }


    }
}
