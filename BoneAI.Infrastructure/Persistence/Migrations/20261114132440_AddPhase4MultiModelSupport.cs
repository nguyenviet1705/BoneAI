using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoneAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase4MultiModelSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AIModels_Name_Version",
                table: "AIModels");

            migrationBuilder.AddColumn<string>(
                name: "Mode",
                table: "Analyses",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "SingleModel");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "AIModels",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<string>(
                name: "ArtifactPath",
                table: "AIModels",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AIModels",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Framework",
                table: "AIModels",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "MockProvider");

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "AIModels",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelKey",
                table: "AIModels",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "mock-bone-fracture-v1");

            migrationBuilder.UpdateData(
                table: "AIModels",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "ArtifactPath", "CreatedAt", "Framework", "IsAvailable", "ModelKey" },
                values: new object[] { null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MockProvider", true, "mock-bone-fracture-v1" });

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_Mode",
                table: "Analyses",
                column: "Mode");

            migrationBuilder.CreateIndex(
                name: "IX_AIModels_IsAvailable",
                table: "AIModels",
                column: "IsAvailable");

            migrationBuilder.CreateIndex(
                name: "IX_AIModels_ModelKey_Version",
                table: "AIModels",
                columns: new[] { "ModelKey", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AIModels_Name_Version",
                table: "AIModels",
                columns: new[] { "Name", "Version" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Analyses_Mode",
                table: "Analyses");

            migrationBuilder.DropIndex(
                name: "IX_AIModels_IsAvailable",
                table: "AIModels");

            migrationBuilder.DropIndex(
                name: "IX_AIModels_ModelKey_Version",
                table: "AIModels");

            migrationBuilder.DropIndex(
                name: "IX_AIModels_Name_Version",
                table: "AIModels");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "Analyses");

            migrationBuilder.DropColumn(
                name: "ArtifactPath",
                table: "AIModels");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AIModels");

            migrationBuilder.DropColumn(
                name: "Framework",
                table: "AIModels");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "AIModels");

            migrationBuilder.DropColumn(
                name: "ModelKey",
                table: "AIModels");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "AIModels",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_AIModels_Name_Version",
                table: "AIModels",
                columns: new[] { "Name", "Version" },
                unique: true);
        }
    }
}
