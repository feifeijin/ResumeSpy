using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResumeSpy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResumeImgPathToResumeDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResumeImgPath",
                table: "ResumeDetails",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResumeImgPath",
                table: "ResumeDetails");
        }
    }
}
