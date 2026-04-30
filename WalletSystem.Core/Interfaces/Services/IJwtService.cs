
using System.Security.Claims;
using WalletSystem.Core.Entities;


namespace WalletSystem.Core.Interfaces.Services
{
    public  interface IJwtService
    {

        (string token, DateTime expiresAt) GenerateToken(User user);
        ClaimsPrincipal? ValidateToken(string token);

    }
}
