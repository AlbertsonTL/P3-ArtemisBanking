using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArtemisBanking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCashierIdToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CashierId",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CashierId",
                table: "Transactions");
        }
    }
}
