using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ResumeSpy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailLoginTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailLoginTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp", nullable: true),
                    RedirectUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    EntryDate = table.Column<DateTime>(type: "timestamp", nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "timestamp", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLoginTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailLoginTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailLoginTokens_UserId_TokenHash",
                table: "EmailLoginTokens",
                columns: new[] { "UserId", "TokenHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailLoginTokens");
        }
    }
}
