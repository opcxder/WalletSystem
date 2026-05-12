using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalletSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TransactionState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Wallet_Status",
                table: "Wallets");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_IdempotencyKey",
                table: "Transactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Transaction_Status",
                table: "Transactions");

            migrationBuilder.AddColumn<DateTime>(
                name: "BankCompletedAt",
                table: "Transactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BankTransactionId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompensationFailureReason",
                table: "Transactions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Transactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRetryAt",
                table: "Transactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAt",
                table: "Transactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Wallet_Status",
                table: "Wallets",
                sql: "[Status] IN ( 'Active','Suspended','Blocked','Deactivated', 'PendingVerification','Closed' )");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BankTransactionId",
                table: "Transactions",
                column: "BankTransactionId",
                unique: true,
                filter: "[BankTransactionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_DestinationWalletId_SourceBankAccountId_Type_IdempotencyKey",
                table: "Transactions",
                columns: new[] { "DestinationWalletId", "SourceBankAccountId", "Type", "IdempotencyKey" },
                unique: true,
                filter: "[Type] = 'AddMoney' AND [DestinationWalletId] IS NOT NULL AND [SourceBankAccountId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transaction_Status",
                table: "Transactions",
                sql: "[Status] IN ( 'Initiated','BankDebitSuccess','Success','Failed', 'WalletCreditSuccess','CompensationPending','CompensationRetrying', 'Compensated','ManualReviewRequired' )");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Wallet_Status",
                table: "Wallets");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_BankTransactionId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_DestinationWalletId_SourceBankAccountId_Type_IdempotencyKey",
                table: "Transactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Transaction_Status",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BankCompletedAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BankTransactionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CompensationFailureReason",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "LastRetryAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "Transactions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Wallet_Status",
                table: "Wallets",
                sql: "[Status] IN ( 'Active','Suspended','Blocked','Deactivated' )");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_IdempotencyKey",
                table: "Transactions",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transaction_Status",
                table: "Transactions",
                sql: "[Status] IN ( 'Initiated','Processing','Success','Failed' )");
        }
    }
}
