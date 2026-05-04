using Microsoft.EntityFrameworkCore;
using SimulatedBank.Entities;
using SimulatedBank.Enums;

namespace SimulatedBank.Data
{
    public class BankContext : DbContext
    {
        public BankContext(DbContextOptions<BankContext> options) : base(options) { }

        public DbSet<Bank> Banks { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Bank
            modelBuilder.Entity<Bank>(entity =>
            {
                entity.HasKey(e => e.BankId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.IFSCCode).IsRequired().HasMaxLength(20);

                entity.HasMany(e => e.BankAccounts)
                      .WithOne(b => b.Bank)
                      .HasForeignKey(b => b.BankId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // BankAccount
            modelBuilder.Entity<BankAccount>(entity =>
            {
                entity.HasKey(e => e.BankAccountId);

                entity.Property(e => e.AccountHolderName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.AccountNumber)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.HasIndex(e => e.AccountNumber)
                      .IsUnique();

                entity.Property(e => e.Balance)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(e => e.AccountType)
                      .HasConversion<int>();


                entity.Property(e => e.IsActive)
                      .HasDefaultValue(true);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("getutcdate()");

                entity.HasMany(e => e.Transactions)
                      .WithOne()
                      .HasForeignKey(t => t.BankAccountId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Transaction
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.TransactionId);

                entity.Property(e => e.Amount)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(e => e.Type).HasConversion<int>();



                entity.Property(e => e.Description)
                      .HasMaxLength(250);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("getutcdate()");
            });

            modelBuilder.Entity<Bank>().HasData(new
            {
                BankId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                Name = "WPay Simulated Bank",
                IFSCCode = "WPAY0001"
            });

            modelBuilder.Entity<BankAccount>().HasData(new
            {
                BankAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                AccountHolderName = "Sunidhi Sharma",
                AccountNumber = "SB1000000001",
                AccountType = AccountType.Savings,
                Balance = 50000m,
                IsActive = true,
                BankId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                BankName = "WPay Simulated Bank",
                CreatedAt = DateTime.UtcNow
            },
            new
            {
                BankAccountId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                AccountHolderName = "Rahul Sharma",
                AccountNumber = "SB1000000002",
                AccountType = AccountType.Savings,
                Balance = 100000m,
                IsActive = true,
                BankId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                BankName = "WPay Simulated Bank",
                CreatedAt = DateTime.UtcNow

            },

             new
             {
                 BankAccountId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                 AccountHolderName = "Sujal Sharma",
                 AccountNumber = "SB1000000003",
                 AccountType = AccountType.Savings,
                 Balance = 750000m,
                 IsActive = true,
                 BankId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                 BankName = "WPay Simulated Bank",
                 CreatedAt = DateTime.UtcNow

             },
              new
              {
                  BankAccountId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                  AccountHolderName = "Priya Reddy",
                  AccountNumber = "SB1000000004",
                  AccountType = AccountType.Savings,
                  Balance = 62000m,
                  IsActive = true,
                  BankId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                  BankName = "WPay Simulated Bank",
                  CreatedAt = new DateTime(2024, 1, 4)
              },
    new
    {
        BankAccountId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
        AccountHolderName = "Arjun Patel",
        AccountNumber = "SB1000000005",
        AccountType = AccountType.Current,
        Balance = 88000m,
        IsActive = true,
        BankId = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f23456789012"),
        BankName = "WPay Simulated Bank",
        CreatedAt = new DateTime(2024, 1, 5)
    }



            );
        }
    }
}