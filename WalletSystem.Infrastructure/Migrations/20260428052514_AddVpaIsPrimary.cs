using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalletSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVpaIsPrimary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrimary",
                table: "Vpas",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPrimary",
                table: "Vpas");
        }
    }
}
