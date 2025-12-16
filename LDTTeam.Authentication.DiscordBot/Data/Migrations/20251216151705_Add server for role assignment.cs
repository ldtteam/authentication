using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LDTTeam.Authentication.DiscordBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class Addserverforroleassignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Server",
                table: "RoleRewards",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Server",
                table: "RoleRewards");
        }
    }
}
