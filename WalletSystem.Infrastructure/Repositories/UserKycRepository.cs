using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalletSystem.Core.Entities;
using WalletSystem.Core.Interfaces.Repositories;
using WalletSystem.Infrastructure.Data;

namespace WalletSystem.Infrastructure.Repositories
{
    public class UserKycRepository : IUserKycRepository
    {

        private readonly WalletContext _walletContext;
        public UserKycRepository(WalletContext walletContext) { 
         _walletContext = walletContext;
         }


        public async Task AddAsync(UserKyc userKyc, CancellationToken ct = default)
        {

            if (userKyc == null)
            {
                throw new ArgumentNullException(nameof(userKyc), "Empty Input");
            }

            await _walletContext.UserKycs.AddAsync(userKyc ,ct);
        }
    }
}
