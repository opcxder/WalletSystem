using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalletSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class user_status_enum_update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_User_Status",
                table: "Users");

            migrationBuilder.AddCheckConstraint(
                name: "CK_User_Status",
                table: "Users",
                sql: "[Status] IN ( 'Active','Suspended','Blocked','Deactivated', 'PendingVerification' )");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_User_Status",
                table: "Users");

            migrationBuilder.AddCheckConstraint(
                name: "CK_User_Status",
                table: "Users",
                sql: "[Status] IN ( 'Active','Suspended','Blocked','Deactivated' )");
        }
    }
}
