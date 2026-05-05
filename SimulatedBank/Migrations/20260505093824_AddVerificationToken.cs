using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SimulatedBank.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificationToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExternalBankAccountId",
                table: "BankAccounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "BankAccounts",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "VerificationTokens",
                columns: table => new
                {
                    TokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(MINUTE, 5, GETUTCDATE())"),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationTokens", x => x.TokenId);
                    table.ForeignKey(
                        name: "FK_VerificationTokens_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "BankAccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Banks",
                columns: new[] { "BankId", "IFSCCode", "Name" },
                values: new object[] { new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), "WPAY0001", "WPay Simulated Bank" });

            migrationBuilder.InsertData(
                table: "BankAccounts",
                columns: new[] { "BankAccountId", "AccountHolderName", "AccountNumber", "AccountType", "Balance", "BankId", "BankName", "CreatedAt", "ExternalBankAccountId", "IsActive" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "Sunidhi Sharma", "SB1000000001", 0, 50000m, new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), "WPay Simulated Bank", new DateTime(2026, 5, 5, 9, 38, 24, 594, DateTimeKind.Utc).AddTicks(6243), null, true },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Rahul Sharma", "SB1000000002", 0, 100000m, new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), "WPay Simulated Bank", new DateTime(2026, 5, 5, 9, 38, 24, 594, DateTimeKind.Utc).AddTicks(6246), new Guid("00000000-0000-0000-0000-000000000000"), true },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Sujal Sharma", "SB1000000003", 0, 750000m, new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), "WPay Simulated Bank", new DateTime(2026, 5, 5, 9, 38, 24, 594, DateTimeKind.Utc).AddTicks(6251), new Guid("00000000-0000-0000-0000-000000000000"), true },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "Priya Reddy", "SB1000000004", 0, 62000m, new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), "WPay Simulated Bank", new DateTime(2024, 1, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new Guid("00000000-0000-0000-0000-000000000000"), true },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "Arjun Patel", "SB1000000005", 1, 88000m, new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), "WPay Simulated Bank", new DateTime(2024, 1, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new Guid("00000000-0000-0000-0000-000000000000"), true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_VerificationTokens_BankAccountId",
                table: "VerificationTokens",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationTokens_TokenHash",
                table: "VerificationTokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerificationTokens");

            migrationBuilder.DeleteData(
                table: "BankAccounts",
                keyColumn: "BankAccountId",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "BankAccounts",
                keyColumn: "BankAccountId",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "BankAccounts",
                keyColumn: "BankAccountId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DeleteData(
                table: "BankAccounts",
                keyColumn: "BankAccountId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));

            migrationBuilder.DeleteData(
                table: "BankAccounts",
                keyColumn: "BankAccountId",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));

            migrationBuilder.DeleteData(
                table: "Banks",
                keyColumn: "BankId",
                keyValue: new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"));

            migrationBuilder.DropColumn(
                name: "ExternalBankAccountId",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "BankAccounts");
        }
    }
}
