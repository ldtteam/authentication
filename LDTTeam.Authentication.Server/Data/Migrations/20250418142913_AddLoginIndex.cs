using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LDTTeam.Authentication.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_LoginProvider",
                table: "AspNetUserLogins",
                column: "LoginProvider");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUserLogins_LoginProvider",
                table: "AspNetUserLogins");
        }
    }
}
