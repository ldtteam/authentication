using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LDTTeam.Authentication.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class Removelegacyinfrastructureandreplaceitwithnewinfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rewards");

            migrationBuilder.CreateTable(
                name: "KnownRewards",
                columns: table => new
                {
                    Type = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Lambda = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnownRewards", x => new { x.Type, x.Name });
                });
            
            migrationBuilder.CreateTable(
                name: "AssignedRewards",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Reward = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignedRewards", x => new { x.UserId, x.Type, x.Reward });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignedRewards");
            
            migrationBuilder.DropTable(
                name: "KnownRewards");

            migrationBuilder.CreateTable(
                name: "Rewards",
                columns: table => new
                {
                    Type = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Lambda = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rewards", x => new { x.Type, x.Name });
                });
        }
    }
}
