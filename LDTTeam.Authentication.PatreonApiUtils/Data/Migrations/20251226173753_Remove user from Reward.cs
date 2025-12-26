using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LDTTeam.Authentication.PatreonApiUtils.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveuserfromReward : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rewards_Users_UserId",
                table: "Rewards");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Memberships_MembershipId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_MembershipId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Rewards_UserId",
                table: "Rewards");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Rewards");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Rewards",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Users_MembershipId",
                table: "Users",
                column: "MembershipId",
                unique: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Memberships_MembershipId",
                table: "Users",
                column: "MembershipId",
                principalTable: "Memberships",
                principalColumn: "MembershipId");
        }
    }
}
