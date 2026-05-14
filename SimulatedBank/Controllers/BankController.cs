
using Microsoft.AspNetCore.Mvc;
using SimulatedBank.Dtos;
using SimulatedBank.Services;

namespace SimulatedBank.Controllers
{
    [Route("api/v1/[controller]/accounts")]
    [ApiController]
    public class BankController : ControllerBase
    {
        private readonly ILogger<BankController> _logger;
        private readonly BankService _bankService;
        public BankController(BankService bankService, ILogger<BankController> logger)
        {
            _bankService = bankService;
            _logger = logger;
        }


        [HttpPost("verify")]
        public async Task<IActionResult> VerifyAccount([FromBody] VerifyAccount VerifyAccountRequest, CancellationToken ct )
        {
           

            if (!ModelState.IsValid)
            {
                var response = new VerifyAccountResponse
                {   
                    Success = false,
                    Message = "Invalid request data"
                };
                return BadRequest(response);
            }

            if(VerifyAccountRequest == null)
            {
                var response = new VerifyAccountResponse
                {
                    Success = false,
                    Message = "Empty Request"
                };
                return BadRequest(response);
            }
            _logger.LogInformation("Verifying account {AccountNumber}", VerifyAccountRequest.AccountNumber);

            try
            {
                var result = await _bankService.VerifyAccountHolder(VerifyAccountRequest, ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error: Bank Controller Verify account method");
                return StatusCode(500, "Something went wrong");
            }
        }

        [HttpPost("link")]
        public async Task<IActionResult> LinkAccount([FromBody] LinkRequest request ,CancellationToken ct)
        {
            _logger.LogInformation("Linking bank account using verification token");

            if (!ModelState.IsValid)
            {
                return BadRequest(new LinkReponse
                {
                    Success = false,
                    Message = "Invalid request data"
                });
            }

            try
            {
                var result = await _bankService.LinkAccount(request , ct);
                _logger.LogInformation("External id we are sending: {id}", result.ExternalReferenceId);

                if (result == null)
                {
                    return NotFound(new LinkReponse
                    {
                        Success = false,
                        Message = "Linking failed"
                    });
                }

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error: Bank Controller Link account method");

                return StatusCode(500, new LinkReponse
                {
                    Success = false,
                    Message = "Something went wrong"
                });
            }
        }


        [HttpGet("check-balance")]
        public async Task<IActionResult> CheckBalance([FromQuery] CheckBalanceRequest request, CancellationToken ct)
        {
            if (request.ExternalBankAccountId == Guid.Empty)
            {
                return BadRequest(new CheckBalanceReponse
                {
                    Success = false,
                    Message = "Refernce ID  is required"
                });
            }

            var result = await _bankService.CheckBalance(request.ExternalBankAccountId, ct);

            if (result == null || !result.Success)
            {
                return NotFound(new CheckBalanceReponse
                {
                    Success = false,
                    Message = "Unable to perform the operation"
                });
            }

            return Ok(result);
        }

        [HttpPost("debit")]
        public async Task<IActionResult> DebitBalance([FromBody] OperationRequest debitRequest,CancellationToken ct = default )
        {
            if(!ModelState.IsValid)
            {
                var response = new OperationResponse
                {
                    Success = false,
                    Message  = "Invalid Request",
                };

               return BadRequest(response);
            }

            var result = await _bankService.DebitAmount(debitRequest.ExternalBankAccountId, debitRequest.Amount,debitRequest.ExternalReferenceId,ct);
            
            
            if(result == null)
            {
                return BadRequest(new OperationResponse
                {
                     Success  = false,
                     Message = "Unable to perform the operation"
                });
            };

            
            if ( !result.Success)
            {
                return BadRequest(new OperationResponse
                {
                    Success = false,
                    ProcessedAt = result.ProcessedAt,
                    TransactionId = result.TransactionId,
                    IsIdempotentReplay = result.IsIdempotentReplay,
                    ExternalReferenceId = result.ExternalReferenceId,
                    ErrorCode = result.ErrorCode,
                    Message = result?.Message ?? "Unable to perform the operation"
                });
            }
            return Ok(result);
        
        }


        [HttpPost("credit")]
        public async Task<IActionResult> CreditBalance([FromBody] OperationRequest creditRequest,CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                var response = new OperationResponse
                {
                    Success = false,
                    Message = "Invalid Request",
                };

                return BadRequest(response);
            }

            

            var result = await _bankService.CreditAmount(creditRequest.ExternalBankAccountId, creditRequest.Amount, creditRequest.ExternalReferenceId,ct);
            if (result == null)
            {
                return BadRequest(new OperationResponse
                {
                    Success = false,
                    Message = "Unable to perform the operation"
                });
            }
            ;


            if (!result.Success)
            {
                return BadRequest(new OperationResponse
                {
                    Success = false,
                    ProcessedAt = result.ProcessedAt,
                    TransactionId = result.TransactionId,
                    IsIdempotentReplay = result.IsIdempotentReplay,
                    ErrorCode = result.ErrorCode,
                    ExternalReferenceId = result.ExternalReferenceId,
                    Message = result?.Message ?? "Unable to perform the operation"
                });
            }
            return Ok(result);
      
        }


    }
}