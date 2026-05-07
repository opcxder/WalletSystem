using Microsoft.EntityFrameworkCore;
using WalletSystem.Core.Entities;
using WalletSystem.Core.Interfaces.Repositories;

namespace WalletSystem.Infrastructure.Data
{
    public class WalletContext : DbContext, IUnitOfWork
    {
        public WalletContext(DbContextOptions<WalletContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<UserCredentials> UserCredentials { get; set; }
        public DbSet<UserKyc> UserKycs { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Vpa> Vpas { get; set; }
        public DbSet<LedgerEntry> LedgerEntries { get; set; }
        public DbSet<LinkedBankAccount> LinkedBankAccounts { get; set; }

      

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //TODO: Make the decimal field value global

            // user
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.UserId);

                entity.Property(u => u.FullName)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(u => u.Email)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(u => u.PhoneNumber)
                      .IsRequired()
                      .HasMaxLength(15);

                entity.Property(u => u.EmailVerificationTokenHash)
                      .HasMaxLength(256);

                entity.Property(u => u.IsEmailVerified)
                      .HasDefaultValue(false);

                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.PhoneNumber).IsUnique();
                entity.HasIndex(u => u.EmailVerificationTokenHash);

                entity.Property(u => u.Status)
                      .HasConversion<string>()
                      .HasMaxLength(20);

                entity.ToTable(u =>
                    u.HasCheckConstraint(
                        "CK_User_Status",
                        "[Status] IN ( 'Active','Suspended','Blocked','Deactivated', 'PendingVerification' )"
                    )
                );
            });

            // user credentials
            modelBuilder.Entity<UserCredentials>(entity =>
            {
                entity.HasKey(u => u.CredentialId);

                entity.Property(u => u.PasswordHash)
                      .IsRequired()
                      .HasMaxLength(256);

                entity.Property(u => u.PaymentPinHash)
                      .HasMaxLength(256);

                entity.HasIndex(c => c.UserId).IsUnique();

                entity.HasOne(c => c.User)
                      .WithOne()
                      .HasForeignKey<UserCredentials>(c => c.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // kyc
            modelBuilder.Entity<UserKyc>(entity =>
            {
                entity.HasKey(k => k.KycId);

                entity.Property(k => k.GovernmentIdNumber)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.Property(k => k.Status)
                      .HasConversion<string>()
                      .HasMaxLength(20);

                entity.Property(k => k.GovernmentIdType)
                      .HasConversion<string>()
                      .HasMaxLength(20);


                entity.ToTable(k =>
                {
                    k.HasCheckConstraint(
                        "CK_UserKyc_GovernmentIdType",
                        "[GovernmentIdType] IN ('Aadhaar', 'PAN' , 'Passport','NotSelected' )"
                    );
                    k.HasCheckConstraint(
                           "CK_UserKyc_Status",
                           "[Status] IN ( 'Pending','Verified','Rejected' )"
                   );
                });

                entity.HasIndex(u => u.UserId).IsUnique();

                entity.HasOne(k => k.User)
                      .WithOne()
                      .HasForeignKey<UserKyc>(k => k.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // wallet
            modelBuilder.Entity<Wallet>(entity =>
            {
                entity.HasKey(w => w.WalletId);

                entity.Property(w => w.Balance)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(w => w.CreatedAt)
                      .HasDefaultValueSql("getutcdate()");

              

                entity.HasIndex(w => w.UserId).IsUnique();

                entity.HasOne(w => w.User)
                      .WithOne()
                      .HasForeignKey<Wallet>(w => w.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(w => w.Status)
                      .HasConversion<string>()
                      .HasMaxLength(20);

                entity.ToTable(w =>
                    w.HasCheckConstraint(
                        "CK_Wallet_Status",
                        "[Status] IN ( 'Active','Suspended','Blocked','Deactivated' )"
                    )
                );
            });

            // vpa
            modelBuilder.Entity<Vpa>(entity =>
            {
                entity.HasKey(v => v.VpaId);

                entity.Property(v => v.VpaAddress)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.HasIndex(v => v.WalletId).IsUnique();

                entity.HasOne(v => v.Wallet)
                      .WithOne()
                      .HasForeignKey<Vpa>(v => v.WalletId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(v => v.VpaAddress).IsUnique();
            });

            // transaction
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(t => t.TransactionId);

                entity.Property(t => t.Amount)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(t => t.Description)
                      .HasMaxLength(250);

                entity.Property(t => t.CreatedAt)
                      .HasDefaultValueSql("getutcdate()");

                entity.HasIndex(t => t.IdempotencyKey).IsUnique();
                entity.HasIndex(t => t.SourceWalletId);
                entity.HasIndex(t => t.DestinationWalletId);
                entity.HasIndex(t => t.CreatedAt);
                entity.HasIndex(t => t.Status);
                entity.HasIndex(t => t.ReferenceId).IsUnique();

                entity.Property(t => t.Status)
                      .HasConversion<string>()
                      .HasMaxLength(20);

                entity.Property(t => t.Type)
                      .HasConversion<string>()
                      .HasMaxLength(30);

                entity.Property(t => t.SourceType)
                      .HasConversion<string>()
                      .HasMaxLength(20);



                entity.HasMany(w => w.LedgerEntries)
                      .WithOne(w => w.Transaction)
                      .HasForeignKey(l => l.TransactionId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.SourceWallet)
                      .WithMany()
                      .HasForeignKey(t => t.SourceWalletId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.DestinationWallet)
                      .WithMany()
                      .HasForeignKey(t => t.DestinationWalletId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint(
                        "CK_Transaction_Source",
                        "(SourceType = 'Wallet' AND SourceWalletId IS NOT NULL AND SourceBankAccountId IS NULL) " +
                        "OR (SourceType = 'Bank' AND SourceWalletId IS NULL AND SourceBankAccountId IS NOT NULL)"
                    );

                    t.HasCheckConstraint(
                        "CK_Transaction_Destination",
                        "(DestinationWalletId IS NOT NULL AND DestinationBankAccountId IS NULL) " +
                        "OR (DestinationWalletId IS NULL AND DestinationBankAccountId IS NOT NULL)"
                    );

                    t.HasCheckConstraint(
                       "CK_Transaction_Type",
                       "[Type] IN ('AddMoney' , 'Transfer' , 'Withdraw')"
                   );
                    t.HasCheckConstraint(
                      "CK_Transaction_SourceType",
                      "[SourceType] IN ('Bank','Wallet')"
                  );
                    t.HasCheckConstraint(
                       "CK_Transaction_Status",
                       "[Status] IN ( 'Initiated','Processing','Success','Failed' )"
                   );
                });
            });

            // ledger
            modelBuilder.Entity<LedgerEntry>(entity =>
            {
                entity.HasKey(l => l.EntryId);

                entity.Property(l => l.Amount)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(l => l.BalanceAfter)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(l => l.CreatedAt)
                      .HasDefaultValueSql("getutcdate()");

                entity.HasIndex(l => l.WalletId);
                entity.HasIndex(l => l.TransactionId);

                entity.Property(l => l.EntryType)
                      .HasConversion<string>()
                      .HasMaxLength(10);
                
               entity.HasIndex(l => new { l.TransactionId, l.WalletId, l.EntryType })
                     .IsUnique();

                entity.HasOne(l => l.Wallet)
                      .WithMany()
                      .HasForeignKey(l => l.WalletId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(l =>
                    l.HasCheckConstraint(
                        "CK_LedgerEntry_EntryType",
                        "[EntryType] IN ('Debit', 'Credit') "
                    )
                );
            });

            modelBuilder.Entity<LinkedBankAccount>(entity =>
            {
                entity.HasKey(l => l.LinkedBankAccountId);

                entity.Property(l => l.IFSCCode)
                      .HasMaxLength(20);

                entity.Property(l => l.MaskedAccountNumber)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.Property(l => l.BankName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(l => l.AccountHolderName)
                      .IsRequired()
                      .HasMaxLength(100);
                      
                      

                entity.Property(l => l.IsDeleted)
                      .HasDefaultValue(false);

                entity.Property(l => l.CreatedAt)
                      .HasDefaultValueSql("getutcdate()");

               

                entity.HasOne(l => l.User)
                      .WithOne()
                      .HasForeignKey<LinkedBankAccount>(l => l.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                // TODO: change when implement multiple bank account per user
              
            });
        }
    }
}