using Microsoft.EntityFrameworkCore;
using SimulatedBank.Entities;

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
        }
    }
}