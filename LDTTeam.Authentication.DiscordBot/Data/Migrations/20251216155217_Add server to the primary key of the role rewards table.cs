using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LDTTeam.Authentication.DiscordBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class Addservertotheprimarykeyoftherolerewardstable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_RoleRewards",
                table: "RoleRewards");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoleRewards",
                table: "RoleRewards",
                columns: new[] { "Reward", "Role", "Server" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_RoleRewards",
                table: "RoleRewards");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoleRewards",
                table: "RoleRewards",
                columns: new[] { "Reward", "Role" });
        }
    }
}
