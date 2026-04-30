

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WalletSystem.Core.Entities;
using System.Security.Cryptography;
using WalletSystem.Core.Interfaces.Services;
using Microsoft.Extensions.Options;
using WalletSystem.Infrastructure.Config;




namespace WalletSystem.Services.Auth
{
    public class JwtService : IJwtService
    {
        private readonly JwtSettings _jwtSettings;

        public JwtService(IOptions<JwtSettings> jwtSetting ) {
          _jwtSettings = jwtSetting.Value;
        }



        public (string token, DateTime expiresAt) GenerateToken(User user )
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);
            var claims =new List<Claim>
            {
                 new Claim(JwtRegisteredClaimNames.Sub , user.UserId.ToString()),
                 new Claim(JwtRegisteredClaimNames.Email , user.Email ?? ""),
                 new Claim("phone" , user.PhoneNumber ?? ""),
                
                 new Claim(JwtRegisteredClaimNames.Jti , Guid.NewGuid().ToString())
               
             };


          using   var rsa = loadRsaKey(_jwtSettings.PrivateKeyPath);
            var key =  new  RsaSecurityKey(rsa);
            var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

            var token = new JwtSecurityToken(
                     issuer: _jwtSettings.Issuer,
                     audience: _jwtSettings.Audience,
                     expires: expiresAt,
                     claims: claims,
                     signingCredentials: credentials
                );
           var tokenString =  new JwtSecurityTokenHandler().WriteToken(token);

            return (tokenString , expiresAt);

        }


        private  RSA loadRsaKey(string rsaKeyPath)
        {
             
            var rsa = RSA.Create();

            if (!File.Exists(rsaKeyPath)) {
                throw new FileNotFoundException("Key file missing", rsaKeyPath);
            }
            var pemContent = File.ReadAllText(rsaKeyPath);
            rsa.ImportFromPem(pemContent);

            return rsa;
        }


        public ClaimsPrincipal? ValidateToken(string token)
        {


            try
            {
               using  var rsa = loadRsaKey(_jwtSettings.PublicKeyPath);
                var tokenhandler = new JwtSecurityTokenHandler();

                var validateParameter = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,        
                    RequireExpirationTime = true,

                    ValidIssuer = _jwtSettings.Issuer,
                    ValidAudience = _jwtSettings.Audience,
                    IssuerSigningKey = new RsaSecurityKey(rsa),

                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenhandler.ValidateToken(token, validateParameter, out _);
                if (principal != null)
                {
                    return principal;
                }
                else
                {
                   return null;
                }



            } catch (Exception ex) {

                throw new Exception("Error while validating token" , ex);
            }

        }
    }
}
