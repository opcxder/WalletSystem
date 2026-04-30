
using System.Security.Cryptography;
using WalletSystem.Core.Interfaces.Services;

namespace WalletSystem.Services.Auth
{
    public class PasswordService : IPasswordService
    {
        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty", nameof(password));

            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string hashPassword, string password)
        {
            if (string.IsNullOrWhiteSpace(hashPassword) || string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Invalid password parameters");

            return BCrypt.Net.BCrypt.Verify(password, hashPassword);
        }


        public string HashToken(string token)
        {
            if(string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Token is empty");

            }
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);

        }

    }
}
