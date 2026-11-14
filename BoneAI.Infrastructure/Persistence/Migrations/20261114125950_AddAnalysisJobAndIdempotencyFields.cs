using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoneAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisJobAndIdempotencyFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LeaseUntil",
                table: "AnalysisRuns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProcessingTimeMs",
                table: "AnalysisRuns",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "AnalysisRuns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Analyses",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.InsertData(
                table: "AIModels",
                columns: new[] { "Id", "Description", "IsActive", "Name", "Version" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "Development and contract-testing mock adapter for pediatric wrist fracture detection", true, "mock-bone-fracture-v1", "1.0.0" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRuns_Status_LeaseUntil",
                table: "AnalysisRuns",
                columns: new[] { "Status", "LeaseUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_IdempotencyKey",
                table: "Analyses",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AnalysisRuns_Status_LeaseUntil",
                table: "AnalysisRuns");

            migrationBuilder.DropIndex(
                name: "IX_Analyses_IdempotencyKey",
                table: "Analyses");

            migrationBuilder.DeleteData(
                table: "AIModels",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DropColumn(
                name: "LeaseUntil",
                table: "AnalysisRuns");

            migrationBuilder.DropColumn(
                name: "ProcessingTimeMs",
                table: "AnalysisRuns");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "AnalysisRuns");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Analyses");
        }
    }
}
