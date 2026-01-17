using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LDTTeam.Authentication.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePrimaryKeyForAssignedRewards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AssignedRewards",
                table: "AssignedRewards");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AssignedRewards",
                table: "AssignedRewards",
                columns: new[] { "UserId", "Type", "Reward" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AssignedRewards",
                table: "AssignedRewards");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AssignedRewards",
                table: "AssignedRewards",
                columns: new[] { "UserId", "Reward" });
        }
    }
}
