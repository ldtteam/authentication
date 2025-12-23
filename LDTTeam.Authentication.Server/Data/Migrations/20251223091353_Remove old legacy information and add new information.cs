using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LDTTeam.Authentication.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class Removeoldlegacyinformationandaddnewinformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConditionInstances");

            migrationBuilder.DropTable(
                name: "HistoricalMetrics");

            migrationBuilder.DropTable(
                name: "Metrics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Rewards",
                table: "Rewards");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Rewards",
                newName: "Lambda");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Rewards",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Rewards",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Rewards",
                table: "Rewards",
                columns: new[] { "Type", "Name" });

            migrationBuilder.CreateTable(
                name: "AssignedRewards",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Reward = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignedRewards", x => new { x.UserId, x.Reward });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignedRewards");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Rewards",
                table: "Rewards");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Rewards");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Rewards");

            migrationBuilder.RenameColumn(
                name: "Lambda",
                table: "Rewards",
                newName: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Rewards",
                table: "Rewards",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ConditionInstances",
                columns: table => new
                {
                    RewardId = table.Column<string>(type: "text", nullable: false),
                    ModuleName = table.Column<string>(type: "text", nullable: false),
                    ConditionName = table.Column<string>(type: "text", nullable: false),
                    LambdaString = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConditionInstances", x => new { x.RewardId, x.ModuleName, x.ConditionName });
                    table.ForeignKey(
                        name: "FK_ConditionInstances_Rewards_RewardId",
                        column: x => x.RewardId,
                        principalTable: "Rewards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Metrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Count = table.Column<long>(type: "bigint", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    Result = table.Column<bool>(type: "boolean", nullable: false),
                    RewardId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Metrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HistoricalMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricId = table.Column<Guid>(type: "uuid", nullable: false),
                    Count = table.Column<long>(type: "bigint", nullable: false),
                    DateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricalMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricalMetrics_Metrics_MetricId",
                        column: x => x.MetricId,
                        principalTable: "Metrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalMetrics_MetricId",
                table: "HistoricalMetrics",
                column: "MetricId");
        }
    }
}
