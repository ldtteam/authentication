using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LDTTeam.Authentication.Modules.Patreon.Data.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatreonMembers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Lifetime = table.Column<long>(type: "bigint", nullable: false),
                    Monthly = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatreonMembers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Token",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Token", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatreonMembers");

            migrationBuilder.DropTable(
                name: "Token");
        }
    }
}
