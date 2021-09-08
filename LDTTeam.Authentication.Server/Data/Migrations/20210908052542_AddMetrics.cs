using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LDTTeam.Authentication.Server.Data.Migrations
{
    public partial class AddMetrics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LambdaString",
                table: "ConditionInstances",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Metrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    RewardId = table.Column<string>(type: "text", nullable: false),
                    Result = table.Column<bool>(type: "boolean", nullable: false),
                    Count = table.Column<long>(type: "bigint", nullable: false)
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
                    DateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Count = table.Column<long>(type: "bigint", nullable: false),
                    MetricId = table.Column<Guid>(type: "uuid", nullable: false)
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoricalMetrics");

            migrationBuilder.DropTable(
                name: "Metrics");

            migrationBuilder.AlterColumn<string>(
                name: "LambdaString",
                table: "ConditionInstances",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
