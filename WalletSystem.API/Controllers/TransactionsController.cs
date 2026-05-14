using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.API.Models;
using WalletSystem.Core.DTOs.Transactions;
using WalletSystem.Core.Interfaces.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WalletSystem.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionsController : BaseController
    {
        private readonly ITransactionService _transactionService;
        private readonly ILogger<TransactionsController> _logger;

        public TransactionsController(ITransactionService transactionService, ILogger<TransactionsController> logger)
        {
            _transactionService = transactionService;
            _logger = logger;
        }



        [HttpGet("{transactionId:guid}")]
        public async Task<IActionResult> GetTransaction( Guid transactionId, CancellationToken ct = default)
        {
            _logger.LogInformation("Getting the Transaction fetching with transaction id  {id}", transactionId);

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid user"));
            }

            if (transactionId == Guid.Empty)
            {
                return BadRequest(ApiResponse<object>.Fail("no transaction id found"));
            }

            var transaction = await _transactionService.GetByIdAsync(userId, transactionId, ct);

            if (!transaction.Success)
            {
                return NotFound(ApiResponse<object>.Fail(transaction.Message ?? "Transaction not found"));
            }

            if (transaction.Result == null)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Unexpected null result"));
            }

            return Ok(ApiResponse<TransactionResponse>.Ok(transaction.Result, "Transaction fetched"));

        }


        [HttpGet("history")]
        public async Task<IActionResult> GetTransactionHistory(CancellationToken ct = default)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid User"));
            }

            var transaction = await _transactionService.GetByUserAsync(userId, ct);

            if (!transaction.Success)
            {
                return NotFound(ApiResponse<object>.Fail(transaction.Message ?? "Transaction not found"));
            }

            if (transaction.Result == null)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Unexpected null result"));
            }

            return Ok(ApiResponse<List<TransactionResponse>>.Ok(transaction.Result, "Transaction Fetched"));
        }



        [HttpPost("send-money")]
        public async Task<IActionResult> SendMoney([FromBody] SendMoneyRequest request, CancellationToken ct = default)
        {

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid User"));
            }

            if (request == null)
            {
                return BadRequest(ApiResponse<object>.Fail("Empty Request"));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);

                foreach(var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Validation  Error: {error}", error.ErrorMessage);
                }
                return BadRequest(ApiResponse<object>.Fail(string.Join(", ", errors)));
            }

            var transaction = await _transactionService.SendMoneyAsync(userId,request , ct);

            if (!transaction.Success)
            {
                return BadRequest(ApiResponse<object>.Fail(transaction.Message ?? "Something went wrong"));
            }

            if(transaction.Result == null)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Unexpected null result"));
            }


            return Ok(ApiResponse<TransactionResponse>.Ok(transaction.Result, "Transaction successful"));


        }


        [HttpPost("add-money")]
        public async Task<IActionResult> AddMoney([FromBody] AddMoneyRequest request , CancellationToken ct = default)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid User"));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);

                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Validation  Error: {error}", error.ErrorMessage);
                }
                return BadRequest(ApiResponse<object>.Fail(string.Join(", ", errors)));
            }

            var transaction = await _transactionService.AddMoneyAsync(userId, request, ct);

            if (!transaction.Success)
            {
                return BadRequest(ApiResponse<object>.Fail(transaction.Message ?? "Something went wrong"));
            }

            if (transaction.Result == null)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Unexpected null result"));
            }


            return Ok(ApiResponse<TransactionResponse>.Ok(transaction.Result , "Transaction successful"));
        }
    }
}
