using Microsoft.EntityFrameworkCore;
using WalletSystem.Core.Entities;
using WalletSystem.Core.Enums;
using WalletSystem.Core.Interfaces.Repositories;
using WalletSystem.Infrastructure.Data;

namespace WalletSystem.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly WalletContext _walletContext;

        public UserRepository(WalletContext walletContext)
        {
            _walletContext = walletContext;
        }

      
        // WRITE OPERATIONS       

        public async Task AddAsync(User user, CancellationToken ct = default)
        {
            await _walletContext.Users.AddAsync(user, ct);
        }

        public Task UpdateAsync(User user, CancellationToken ct = default)
        {
            _walletContext.Users.Update(user);
            return Task.CompletedTask;
        }
     
        // READ - ACTIVE USERS ONLY
        // (For login, signup, validation)
     

        public async Task<User?> GetActiveByEmailAsync(string email, CancellationToken ct = default)
        {
            return await _walletContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.Email == email &&
                    u.Status != UserStatus.Deactivated, ct);
        }

        public async Task<User?> GetActiveByPhoneAsync(string phone, CancellationToken ct = default)
        {
            return await _walletContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.PhoneNumber == phone &&
                    u.Status != UserStatus.Deactivated, ct);
        }

        public async Task<bool> ExistsActiveByEmailAsync(string email, CancellationToken ct = default)
        {
            return await _walletContext.Users
                .AnyAsync(u =>
                    u.Email == email &&
                    u.Status != UserStatus.Deactivated, ct);
        }

        public async Task<bool> ExistsActiveByPhoneAsync(string phone, CancellationToken ct = default)
        {
            return await _walletContext.Users
                .AnyAsync(u =>
                    u.PhoneNumber == phone &&
                    u.Status != UserStatus.Deactivated, ct);
        }


        public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        {
            return await _walletContext.Users
                .AnyAsync(u => u.Email == email, ct);
        }

        public async Task<bool> ExistsByPhoneAsync(string phone, CancellationToken ct = default)
        {
            return await _walletContext.Users
                .AnyAsync(u => u.PhoneNumber == phone, ct);
        }



        // READ - ANY STATUS
        // (For transactions, admin, audit)

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _walletContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == id, ct);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            return await _walletContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email, ct);
        }

        public async Task<User?> GetByPhoneAsync(string phone, CancellationToken ct = default)
        {
            return await _walletContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.PhoneNumber == phone, ct);
        }

        // TRACKED ENTITY (FOR UPDATE)

        public async Task<User?> GetByEmailAsyncForUpdate(string email, CancellationToken ct = default)
        {
            return await _walletContext.Users
               .FirstOrDefaultAsync(u => u.Email == email, ct);
        }
        public async Task<User?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default)
        {
            return await _walletContext.Users
                .FirstOrDefaultAsync(u => u.UserId == id, ct);
        }

       public async Task<User?> GetByEmailVerificationTokenHashAsync(string tokenHash, CancellationToken ct = default) 
        {
            return await _walletContext.Users.FirstOrDefaultAsync(u => u.EmailVerificationTokenHash == tokenHash
              && u.Status == UserStatus.PendingVerification, ct);
        }

    }
}