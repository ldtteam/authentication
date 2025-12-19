using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LDTTeam.Authentication.RewardAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class Createrelationshipsbetweenentities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AssignedRewards_Reward_Type",
                table: "AssignedRewards",
                columns: new[] { "Reward", "Type" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AssignedRewards_Users_UserId",
                table: "AssignedRewards",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Logins_Users_UserId",
                table: "Logins",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssignedRewards_Users_UserId",
                table: "AssignedRewards");

            migrationBuilder.DropForeignKey(
                name: "FK_Logins_Users_UserId",
                table: "Logins");

            migrationBuilder.DropIndex(
                name: "IX_AssignedRewards_Reward_Type",
                table: "AssignedRewards");
        }
    }
}
