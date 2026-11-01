using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ChoresApi.Migrations
{
    /// <inheritdoc />
    public partial class SplitEntriesIntoClaimsAndCompletions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChoreClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChoreId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChoreClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChoreClaims_Chores_ChoreId",
                        column: x => x.ChoreId,
                        principalTable: "Chores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChoreClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChoreCompletions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChoreId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChoreCompletions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChoreCompletions_Chores_ChoreId",
                        column: x => x.ChoreId,
                        principalTable: "Chores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChoreCompletions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChoreClaims_ChoreId_Date",
                table: "ChoreClaims",
                columns: new[] { "ChoreId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_ChoreClaims_UserId",
                table: "ChoreClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChoreCompletions_ChoreId_Date",
                table: "ChoreCompletions",
                columns: new[] { "ChoreId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_ChoreCompletions_UserId",
                table: "ChoreCompletions",
                column: "UserId");

            // Ticked-off entries become completions, the rest claims.
            migrationBuilder.Sql("""
                INSERT INTO "ChoreCompletions" ("ChoreId", "UserId", "Date", "CreatedAt")
                SELECT "ChoreId", "UserId", "Date", "CompletedAt" FROM "ChoreEntries" WHERE "CompletedAt" IS NOT NULL;
                INSERT INTO "ChoreClaims" ("ChoreId", "UserId", "Date", "CreatedAt")
                SELECT "ChoreId", "UserId", "Date", "CreatedAt" FROM "ChoreEntries" WHERE "CompletedAt" IS NULL;
                """);

            migrationBuilder.DropTable(
                name: "ChoreEntries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChoreEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChoreId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChoreEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChoreEntries_Chores_ChoreId",
                        column: x => x.ChoreId,
                        principalTable: "Chores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChoreEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChoreEntries_ChoreId_Date",
                table: "ChoreEntries",
                columns: new[] { "ChoreId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_ChoreEntries_UserId",
                table: "ChoreEntries",
                column: "UserId");

            migrationBuilder.Sql("""
                INSERT INTO "ChoreEntries" ("ChoreId", "UserId", "Date", "CompletedAt", "CreatedAt")
                SELECT "ChoreId", "UserId", "Date", "CreatedAt", "CreatedAt" FROM "ChoreCompletions";
                INSERT INTO "ChoreEntries" ("ChoreId", "UserId", "Date", "CompletedAt", "CreatedAt")
                SELECT "ChoreId", "UserId", "Date", NULL, "CreatedAt" FROM "ChoreClaims";
                """);

            migrationBuilder.DropTable(
                name: "ChoreClaims");

            migrationBuilder.DropTable(
                name: "ChoreCompletions");
        }
    }
}
