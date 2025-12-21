using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LDTTeam.Authentication.PatreonApiUtils.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRewardrelationshipwithUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rewards_Users_MembershipId",
                table: "Rewards");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Rewards",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_UserId",
                table: "Rewards",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rewards_Users_UserId",
                table: "Rewards",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rewards_Users_UserId",
                table: "Rewards");

            migrationBuilder.DropIndex(
                name: "IX_Rewards_UserId",
                table: "Rewards");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Rewards");

            migrationBuilder.AddForeignKey(
                name: "FK_Rewards_Users_MembershipId",
                table: "Rewards",
                column: "MembershipId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
