using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalletSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBalanceAfter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BalanceBefore",
                table: "LedgerEntries",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BalanceBefore",
                table: "LedgerEntries");
        }
    }
}
