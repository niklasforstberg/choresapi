using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChoresApi.Migrations
{
    /// <inheritdoc />
    public partial class CompletionRecorderNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChoreCompletions_Users_RecordedById",
                table: "ChoreCompletions");

            migrationBuilder.AlterColumn<int>(
                name: "RecordedById",
                table: "ChoreCompletions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_ChoreCompletions_Users_RecordedById",
                table: "ChoreCompletions",
                column: "RecordedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChoreCompletions_Users_RecordedById",
                table: "ChoreCompletions");

            migrationBuilder.AlterColumn<int>(
                name: "RecordedById",
                table: "ChoreCompletions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ChoreCompletions_Users_RecordedById",
                table: "ChoreCompletions",
                column: "RecordedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
