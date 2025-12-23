using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LDTTeam.Authentication.RewardAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class Removetheindexfortherewardandtypeontheassignedreward : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssignedRewards_Reward_Type",
                table: "AssignedRewards");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AssignedRewards_Reward_Type",
                table: "AssignedRewards",
                columns: new[] { "Reward", "Type" },
                unique: true);
        }
    }
}
