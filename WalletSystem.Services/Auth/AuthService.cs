

using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using WalletSystem.Core.common;
using WalletSystem.Core.DTOs.Auth;
using WalletSystem.Core.Entities;
using WalletSystem.Core.Enums;
using WalletSystem.Core.Interfaces.Repositories;
using WalletSystem.Core.Interfaces.Services;

namespace WalletSystem.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserCredentialsRepository _credentialsRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly IPasswordService _passwordService;
        private readonly IJwtService _jwtService;
        private readonly IUserKycRepository _userKycRepository;
        private readonly ILogger<AuthService> _logger;
        public AuthService(IUserRepository userRepository,
        IUserCredentialsRepository credentialsRepository,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IPasswordService passwordService,
        IUserKycRepository userKycRepository,
        IJwtService jwtService,
        ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _credentialsRepository = credentialsRepository;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _passwordService = passwordService;
            _userKycRepository = userKycRepository;
            _jwtService = jwtService;
            _logger = logger;
        }


        public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request)
        {
            if (request == null)
            {
                return ServiceResult<AuthResponse>.Fail("Invalid credentials");

            }

            var hasEmail = !string.IsNullOrWhiteSpace(request.Email);
            var hasPhone = !string.IsNullOrWhiteSpace(request.PhoneNumber);


            if (hasEmail && hasPhone)
                return ServiceResult<AuthResponse>.Fail("Provide either email or phone, not both");

            if (!hasEmail && !hasPhone)
                return ServiceResult<AuthResponse>.Fail("Email or phone is required");

            request.Email = request.Email?.Trim().ToLower();
            request.PhoneNumber = request.PhoneNumber?.Trim();
            var identifier = hasEmail
                                    ? request.Email
                                    : request.PhoneNumber;

            if (string.IsNullOrWhiteSpace(identifier))
            {
                return ServiceResult<AuthResponse>.Fail("Email or Phone is required");
            }

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            {
                return ServiceResult<AuthResponse>.Fail("Password is  required and should be more than 6 character");
            }

            var loginType = DetectLoginType(identifier);

            if (loginType == LoginType.Invalid)
            {
                _logger.LogWarning("Invalid login type detected for identifier: {Identifier}", identifier);
                return ServiceResult<AuthResponse>.Fail("Invalid credential");
            }
            User? user = loginType == LoginType.Email
                        ? await _userRepository.GetActiveByEmailAsync(identifier)
                        : await _userRepository.GetActiveByPhoneAsync(identifier);

            if (user == null)
            {
                return ServiceResult<AuthResponse>.Fail("Invalid credentials");

            }

            if (!user.IsEmailVerified)
            {
                return ServiceResult<AuthResponse>.Fail("Email not verified");
            }

            if (user.Status != UserStatus.Active)
            {
                return ServiceResult<AuthResponse>.Fail("Invalid credentials");
            }

            var credentials = await _credentialsRepository.GetByUserIdAsync(user.UserId);

            if (credentials == null)
            {
                return ServiceResult<AuthResponse>.Fail("Invalid credentials");
            }

            if (!_passwordService.VerifyPassword(credentials.PasswordHash, request.Password))
            {
                return ServiceResult<AuthResponse>.Fail("Invalid credentials");

            }

            var token = _jwtService.GenerateToken(user);

            //TODO: fetch the wallet information and add vpa address in the response.
            _logger.LogInformation("User logged in successfully: {UserId}", user.UserId);   
            return ServiceResult<AuthResponse>.Ok(new AuthResponse
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Token = token.token,
                ExpiresAt = token.expiresAt,
            });

        }

        public async Task<ServiceResult<RegisterResponse>> RegisterAsync(RegisterRequest request)
        {
            if (request == null)
            {
                return ServiceResult<RegisterResponse>.Fail("Request is empty");
            }

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            {
                return ServiceResult<RegisterResponse>.Fail("Password is  required and should be more than 6 character");
            }

            request.Email = request.Email.Trim().ToLower();
            request.PhoneNumber = request.PhoneNumber.Trim();
            var mailCheck = await _userRepository.ExistsByEmailAsync(request.Email);
            if (mailCheck == true)
            {
                _logger.LogInformation("Registration failed: email already exists {Email}", request.Email);
                return ServiceResult<RegisterResponse>.Fail("Email is already in the system");
            }

            var phoneCheck = await _userRepository.ExistsByPhoneAsync(request.PhoneNumber);
            if (phoneCheck == true)
            {
                _logger.LogInformation("Registration failed: phone exists {Phone}", request.PhoneNumber);
                return ServiceResult<RegisterResponse>.Fail("Phone number already in the system");
            }

            var token = GenerateEmailVerificationToken();
            var hashedToken = _passwordService.HashToken(token);

            var user = new User
            {
                UserId = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Status = UserStatus.PendingVerification,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsEmailVerified = false,
                EmailVerificationTokenHash = hashedToken,
                EmailVerificationTokenExpiry = DateTime.UtcNow.AddMinutes(15)
            };

            var credentials = new UserCredentials
            {
                CredentialId = Guid.NewGuid(),
                UserId = user.UserId,
                PasswordHash = _passwordService.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var userKyc = new UserKyc
            {
                KycId = Guid.NewGuid(),
                UserId  = user.UserId,
                GovernmentIdType = request.GovernmentIdType,
                GovernmentIdNumber = request.GovernmentIdNumber,
                Status = KycStatus.Pending,
                CreatedAt = DateTime.UtcNow,
            };

            await _userRepository.AddAsync(user);
            await _credentialsRepository.AddAsync(credentials);
            await _userKycRepository.AddAsync(userKyc);

            try
            {
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("User registered successfully: {UserId}", user.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save user during registration");
                return ServiceResult<RegisterResponse>.Fail("Registration failed. Please try again.");
            }

            var verificationUrl = $"https://localhost:5001/api/v1/auth/verify-email?token={Uri.EscapeDataString(token)}";


            var emailSent = true;
            try
            {
                await _emailService.SendMailAsync(user.Email, "Verify your MailId", BuildVerificationEmailHtml(user.FullName, verificationUrl));
            }
            catch (Exception ex)
            {
                emailSent = false;
                _logger.LogError(ex, "Failed to send verification email");

            }

            return ServiceResult<RegisterResponse>.Ok(new RegisterResponse
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                IsEmailVerified = false,
                EmailSent = emailSent,
                Message = emailSent
         ? "Registration successful. Please check your email to verify your account."
         : "Account created but verification email failed. Please use resend verification."
            });

        }


        public async Task<ServiceResult> VerifyEmailAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return ServiceResult.Fail("Invalid token");
            }


            var hashToken = _passwordService.HashToken(token);
           

            var user = await _userRepository.GetByEmailVerificationTokenHashAsync(hashToken);
            if (user == null)
            {
                return ServiceResult.Fail("Invalid token");
            }
            if (user.EmailVerificationTokenExpiry == null ||
                user.EmailVerificationTokenExpiry < DateTime.UtcNow)
            {
                return ServiceResult.Fail("Token expired");
            }

            if (user.IsEmailVerified)
                return ServiceResult.Ok("Already verified");

            user.IsEmailVerified = true;
            user.Status = UserStatus.Active;
            user.EmailVerifiedAt = DateTime.UtcNow;
            user.EmailVerificationTokenHash = null;
            user.EmailVerificationTokenExpiry = null;

            try
            {
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Email verified successfully: {UserId}", user.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while verifying the user mail");
                return ServiceResult.Fail("Email verification failed");

            }


            return ServiceResult.Ok("Email Verified");

        }


        public async Task<ServiceResult> ResendVerificationEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return ServiceResult.Fail("Email is empty");
            }
            email = email.Trim().ToLower();
            User? user = await _userRepository.GetByEmailAsyncForUpdate(email);
            if (user == null)
            {
                return ServiceResult.Fail("User not found");
            }


            if (user.IsEmailVerified || user.Status != UserStatus.PendingVerification)
                return ServiceResult.Fail("User already verified");


            var token = GenerateEmailVerificationToken();
            var hashtoken = _passwordService.HashToken(token);



            user.EmailVerificationTokenHash = hashtoken;
            user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddMinutes(15);

            var verificationUrl = $"https://localhost:5001/v1/api/auth/verify-email?token={Uri.EscapeDataString(token)}";




            try
            {
                await _emailService.SendMailAsync(user.Email, "Re: Verify your mail", BuildVerificationEmailHtml(user.FullName, verificationUrl));
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error: AuthService Resend Mail");
                return ServiceResult.Fail("Error while sending mail");
            }

            return ServiceResult.Ok("Email Sent");


        }


        private  static string GenerateEmailVerificationToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes);
        }

        private static LoginType DetectLoginType(string loginType)
        {
            if (string.IsNullOrEmpty(loginType))
            {
                return LoginType.Invalid;
            }

            loginType = loginType.Trim().ToLower();

            if (IsEmail(loginType))
            {
                return LoginType.Email;
            }
            if (IsPhone(loginType))
            {
                return LoginType.Phone;
            }
            return LoginType.Invalid;
        }

        private  static bool IsEmail(string input)
        {
            return Regex.IsMatch(input, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        private static  bool IsPhone(string input)
        {
            return Regex.IsMatch(input, @"^\+?[0-9]{10,15}$");
        }


        private static  string BuildVerificationEmailHtml(string fullName, string verificationUrl)
        {
            return $"""
        <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
            <h2 style="color: #2E75B6;">Verify your WPay account</h2>
            <p>Hi {fullName},</p>
            <p>Click the button below to verify your email address.</p>
            <a href="{verificationUrl}" 
               style="background-color: #2E75B6; color: white; padding: 12px 24px; 
                      text-decoration: none; border-radius: 4px; display: inline-block;">
                Verify Email
            </a>
            <p style="color: #666; font-size: 12px;">This link expires in 15 minutes.</p>
            <p style="color: #666; font-size: 12px;">If you did not create a WPay account, ignore this email.</p>
        </div>
        """;
        }

    }
}
