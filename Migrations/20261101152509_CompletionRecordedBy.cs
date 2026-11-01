using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChoresApi.Migrations
{
    /// <inheritdoc />
    public partial class CompletionRecordedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RecordedById",
                table: "ChoreCompletions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Until now members only recorded their own.
            migrationBuilder.Sql("""UPDATE "ChoreCompletions" SET "RecordedById" = "UserId";""");

            migrationBuilder.CreateIndex(
                name: "IX_ChoreCompletions_RecordedById",
                table: "ChoreCompletions",
                column: "RecordedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ChoreCompletions_Users_RecordedById",
                table: "ChoreCompletions",
                column: "RecordedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChoreCompletions_Users_RecordedById",
                table: "ChoreCompletions");

            migrationBuilder.DropIndex(
                name: "IX_ChoreCompletions_RecordedById",
                table: "ChoreCompletions");

            migrationBuilder.DropColumn(
                name: "RecordedById",
                table: "ChoreCompletions");
        }
    }
}
