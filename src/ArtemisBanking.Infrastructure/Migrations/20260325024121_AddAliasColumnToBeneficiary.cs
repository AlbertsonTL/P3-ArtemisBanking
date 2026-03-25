using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArtemisBanking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAliasColumnToBeneficiary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Alias",
                table: "Beneficiaries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Alias",
                table: "Beneficiaries");
        }
    }
}
