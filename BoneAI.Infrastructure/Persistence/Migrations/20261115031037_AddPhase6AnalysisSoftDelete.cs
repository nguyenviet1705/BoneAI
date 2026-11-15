using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoneAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase6AnalysisSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Analyses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Analyses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_IsDeleted",
                table: "Analyses",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Analyses_IsDeleted",
                table: "Analyses");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Analyses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Analyses");
        }
    }
}
