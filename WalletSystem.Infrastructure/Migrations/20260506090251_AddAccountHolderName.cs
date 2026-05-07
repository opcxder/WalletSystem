using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalletSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountHolderName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MaskedAccountNumber",
                table: "LinkedBankAccounts",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4)",
                oldMaxLength: 4);

            migrationBuilder.AddColumn<string>(
                name: "AccountHolderName",
                table: "LinkedBankAccounts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountHolderName",
                table: "LinkedBankAccounts");

            migrationBuilder.AlterColumn<string>(
                name: "MaskedAccountNumber",
                table: "LinkedBankAccounts",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);
        }
    }
}
