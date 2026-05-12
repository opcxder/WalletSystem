

using Microsoft.EntityFrameworkCore;
using WalletSystem.Core.Entities;
using WalletSystem.Core.Interfaces.Repositories;
using WalletSystem.Infrastructure.Data;

namespace WalletSystem.Infrastructure.Repositories
{
    public class VpaRepository : IVpaRepository
    {

        private readonly WalletContext _walletContext;

        public VpaRepository(WalletContext walletContext) 
        {
            _walletContext = walletContext;
        }


        public async Task AddAsync(Vpa vpa, CancellationToken ct = default)
        {
              if(vpa == null) throw new ArgumentNullException(nameof(vpa));
               await _walletContext.Vpas.AddAsync(vpa, ct);
        }

        public async Task<bool> ExistsAsync(string vpaAddress, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(vpaAddress)) throw new ArgumentNullException(nameof(vpaAddress));
              var res =  await _walletContext.Vpas.FirstOrDefaultAsync(x => x.VpaAddress  == vpaAddress,ct);
             return res == null ? false : true;
        }

        public async Task<Vpa?> GetByAddressAsync(string vpaAddress, CancellationToken ct = default)
        {
            if(string.IsNullOrWhiteSpace(vpaAddress)) throw new ArgumentNullException(nameof(vpaAddress));
            return await _walletContext.Vpas.FirstOrDefaultAsync(x => x.VpaAddress == vpaAddress , ct);
        }

        public async Task<Vpa?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty) throw new ArgumentException("Invalid id", nameof(id));
            return await _walletContext.Vpas.FirstOrDefaultAsync(x => x.VpaId == id, ct);
        }

        public async Task<Vpa?> GetByWalletIdAsync(Guid walletId, CancellationToken ct = default)
        {
            if (walletId == Guid.Empty) throw new ArgumentException("Invalid id", nameof(walletId));

            return await _walletContext.Vpas.FirstOrDefaultAsync(x => x.WalletId == walletId, ct);
        }
    }
}
