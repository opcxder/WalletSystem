using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimulatedBank.Migrations
{
    /// <inheritdoc />
    public partial class AddBankTransactionIdempotencyAndConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Transactions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Transactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ErrorCode",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ExternalReferenceId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Transactions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "AccountType",
                table: "BankAccounts",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.UpdateData(
                table: "BankAccounts",
                keyColumn: "BankAccountId",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "AccountType", "CreatedAt" },
                values: new object[] { "Savings", new DateTime(2026, 5, 11, 10, 44, 1, 60, DateTimeKind.Utc).AddTicks(6622) });

            migrationBuilder.UpdateData(
                table: "BankAccounts",
                keyColumn: "BankAccountId",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "AccountType", "CreatedAt" },
                values: new object[] { "Savings", new DateTime(2026, 5, 11, 10, 44, 1, 60, DateTimeKind.Utc).AddTicks(6627) });

            migrationBuilder.UpdateData(
                table: "BankAccounts",
                keyColumn: "BankAccountId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "AccountType", "CreatedAt" },
                values: new object[] { "Savings", new DateTime(2026, 5, 11, 10, 44, 1, 60, DateTimeKind.Utc).AddTicks(6631) });

            migrationBuilder.UpdateData(
                table: "BankAccounts",
                keyColumn: "BankAccountId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "AccountType",
                value: "Savings");

            migrationBuilder.UpdateData(
                table: "BankAccounts",
                keyColumn: "BankAccountId",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "AccountType",
                value: "Current");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ExternalReferenceId_Type",
                table: "Transactions",
                columns: new[] { "ExternalReferenceId", "Type" },
                unique: true,
                filter: "[ExternalReferenceId] != '00000000-0000-0000-0000-000000000000'");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_ExternalBankAccountId",
                table: "BankAccounts",
                column: "ExternalBankAccountId",
                unique: true,
                filter: "[ExternalBankAccountId] IS NOT NULL AND [ExternalBankAccountId] != '00000000-0000-0000-0000-000000000000'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_ExternalReferenceId_Type",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_BankAccounts_ExternalBankAccountId",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ErrorCode",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ExternalReferenceId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Transactions");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Transactions",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<int>(
                name: "AccountType",
                table: "BankAccounts",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.UpdateData(
                table: "BankAccounts",
                keyColumn: "BankAccountId",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "AccountType", "CreatedAt" },
                values: new object[] { 0, new DateTime(2026, 5, 5, 9, 38, 24, 594, DateTimeKind.Utc).AddTicks(6243) });

            migrationBuilder.UpdateData(
                table: "BankAccounts",
                keyColumn: "BankAccountId",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "AccountType", "CreatedAt" },
                values: new object[] { 0, new DateTime(2026, 5, 5, 9, 38, 24, 594, DateTimeKind.Utc).AddTicks(6246) });

            migrationBuilder.UpdateData(
                table: "BankAccounts",
                keyColumn: "BankAccountId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "AccountType", "CreatedAt" },
                values: new object[] { 0, new DateTime(2026, 5, 5, 9, 38, 24, 594, DateTimeKind.Utc).AddTicks(6251) });

            migrationBuilder.UpdateData(
                table: "BankAccounts",
                keyColumn: "BankAccountId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "AccountType",
                value: 0);

            migrationBuilder.UpdateData(
                table: "BankAccounts",
                keyColumn: "BankAccountId",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "AccountType",
                value: 1);
        }
    }
}
