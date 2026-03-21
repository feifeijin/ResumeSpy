using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResumeSpy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnonymousUserSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resumes_GuestSessions_GuestSessionId",
                table: "Resumes");

            migrationBuilder.DropTable(
                name: "GuestSessions");

            migrationBuilder.RenameColumn(
                name: "GuestSessionId",
                table: "Resumes",
                newName: "AnonymousUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Resumes_UserId_GuestSessionId",
                table: "Resumes",
                newName: "IX_Resumes_UserId_AnonymousUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Resumes_GuestSessionId",
                table: "Resumes",
                newName: "IX_Resumes_AnonymousUserId");

            migrationBuilder.CreateTable(
                name: "AnonymousUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResumeCount = table.Column<int>(type: "integer", nullable: false),
                    IsConverted = table.Column<bool>(type: "boolean", nullable: false),
                    ConvertedUserId = table.Column<string>(type: "text", nullable: true),
                    EntryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnonymousUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnonymousUsers_Users_ConvertedUserId",
                        column: x => x.ConvertedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnonymousUsers_ConvertedUserId",
                table: "AnonymousUsers",
                column: "ConvertedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AnonymousUsers_IsConverted",
                table: "AnonymousUsers",
                column: "IsConverted");

            migrationBuilder.AddForeignKey(
                name: "FK_Resumes_AnonymousUsers_AnonymousUserId",
                table: "Resumes",
                column: "AnonymousUserId",
                principalTable: "AnonymousUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resumes_AnonymousUsers_AnonymousUserId",
                table: "Resumes");

            migrationBuilder.DropTable(
                name: "AnonymousUsers");

            migrationBuilder.RenameColumn(
                name: "AnonymousUserId",
                table: "Resumes",
                newName: "GuestSessionId");

            migrationBuilder.RenameIndex(
                name: "IX_Resumes_UserId_AnonymousUserId",
                table: "Resumes",
                newName: "IX_Resumes_UserId_GuestSessionId");

            migrationBuilder.RenameIndex(
                name: "IX_Resumes_AnonymousUserId",
                table: "Resumes",
                newName: "IX_Resumes_GuestSessionId");

            migrationBuilder.CreateTable(
                name: "GuestSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConvertedUserId = table.Column<string>(type: "text", nullable: true),
                    EntryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    IsConverted = table.Column<bool>(type: "boolean", nullable: false),
                    ResumeCount = table.Column<int>(type: "integer", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestSessions_Users_ConvertedUserId",
                        column: x => x.ConvertedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuestSessions_ConvertedUserId",
                table: "GuestSessions",
                column: "ConvertedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestSessions_ExpiresAt",
                table: "GuestSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_GuestSessions_IsConverted",
                table: "GuestSessions",
                column: "IsConverted");

            migrationBuilder.AddForeignKey(
                name: "FK_Resumes_GuestSessions_GuestSessionId",
                table: "Resumes",
                column: "GuestSessionId",
                principalTable: "GuestSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
