using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoneAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase5ResultImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResultImagePath",
                table: "AnalysisRuns",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResultImagePath",
                table: "AnalysisRuns");
        }
    }
}
