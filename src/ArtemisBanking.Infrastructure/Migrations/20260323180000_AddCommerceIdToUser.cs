using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArtemisBanking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommerceIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CommerceId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CommerceId",
                table: "AspNetUsers",
                column: "CommerceId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Commerces_CommerceId",
                table: "AspNetUsers",
                column: "CommerceId",
                principalTable: "Commerces",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Commerces_CommerceId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CommerceId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CommerceId",
                table: "AspNetUsers");
        }
    }
}
