


using Microsoft.AspNetCore.Mvc;
using WalletSystem.API.Models;
using WalletSystem.Core.DTOs.Auth;
using WalletSystem.Core.Interfaces.Services;

namespace WalletSystem.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            _logger.LogInformation("Register attempt for {Email}", request.Email);
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                   .SelectMany(v => v.Errors)
                   .Select(e => e.ErrorMessage);
                return BadRequest(ApiResponse<object>.Fail(string.Join(", ", errors)));
            }


            if (   request == null  )
            {
                return BadRequest(ApiResponse<object>.Fail("Empty Input"));
            }

            


            var result = await _authService.RegisterAsync(request);


            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.Fail(result.Message ?? "Operation failed"));
            }


            var data = result.Result!;
            return StatusCode(201, ApiResponse<RegisterResponse>.Ok(
                                 data,
                                 "Registration successful"
                            ));
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {

            _logger.LogInformation("Login attempt received");
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                   .SelectMany(v => v.Errors)
                   .Select(e => e.ErrorMessage);
                return BadRequest(ApiResponse<object>.Fail(string.Join(", ", errors)));
            }
            var result = await _authService.LoginAsync(request);

            if (!result.Success)
            {
                return Unauthorized(ApiResponse<object>.Fail(result.Message ?? "Operation failed"));
            }
            var data = result.Result!;
            return Ok(ApiResponse<AuthResponse>.Ok(
                      data,
                      "Login successful"
                    ));

        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            var result = await _authService.VerifyEmailAsync(token);

            if (!result.Success)
            {

                return BadRequest(ApiResponse.Fail(result.Message ?? "Operation failed"));
            }


            return Ok(ApiResponse.Ok("Email verified successfully"));

        }

        [HttpPost("resend-verification")]
        public async Task<IActionResult> Resend([FromBody] ResendVerificationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Invalid request"));

            var result = await _authService.ResendVerificationEmailAsync(request.Email);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Message ?? "Operation failed"));

            return Ok(ApiResponse.Ok("Email verification sent"));
        }

    }
}
