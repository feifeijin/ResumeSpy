using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResumeSpy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResumeVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResumeVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResumeDetailId = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResumeVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResumeVersions_ResumeDetails_ResumeDetailId",
                        column: x => x.ResumeDetailId,
                        principalTable: "ResumeDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResumeVersions_CreatedAt",
                table: "ResumeVersions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ResumeVersions_ResumeDetailId",
                table: "ResumeVersions",
                column: "ResumeDetailId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResumeVersions");
        }
    }
}
