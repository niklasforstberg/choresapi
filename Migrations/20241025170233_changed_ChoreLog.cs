using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChoresApi.Migrations
{
    /// <inheritdoc />
    public partial class changed_ChoreLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "ChoresLog");

            migrationBuilder.RenameColumn(
                name: "DueDate",
                table: "ChoresLog",
                newName: "CompletedDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CompletedDate",
                table: "ChoresLog",
                newName: "DueDate");

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "ChoresLog",
                type: "INTEGER",
                nullable: true);
        }
    }
}
