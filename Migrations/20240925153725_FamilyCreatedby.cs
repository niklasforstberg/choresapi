using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChoresApi.Migrations
{
    /// <inheritdoc />
    public partial class FamilyCreatedby : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Families",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ChoreUsers_Email",
                table: "ChoreUsers",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChoreUsers_Email",
                table: "ChoreUsers");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Families");
        }
    }
}
