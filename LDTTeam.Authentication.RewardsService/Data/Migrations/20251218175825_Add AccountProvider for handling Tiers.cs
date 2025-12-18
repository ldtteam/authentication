using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LDTTeam.Authentication.RewardsService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountProviderforhandlingTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TierAssignments",
                table: "TierAssignments");

            migrationBuilder.AddColumn<int>(
                name: "Provider",
                table: "TierAssignments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TierAssignments",
                table: "TierAssignments",
                columns: new[] { "UserId", "Provider", "Tier" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TierAssignments",
                table: "TierAssignments");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "TierAssignments");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TierAssignments",
                table: "TierAssignments",
                columns: new[] { "UserId", "Tier" });
        }
    }
}
