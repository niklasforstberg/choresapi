using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChoresApi.Migrations
{
    /// <inheritdoc />
    public partial class Chorelognewfield : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChoresLog_ChoreUsers_ChoreUserId",
                table: "ChoresLog");

            migrationBuilder.AlterColumn<bool>(
                name: "IsCompleted",
                table: "ChoresLog",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "ChoresLog",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "ChoreUserId",
                table: "ChoresLog",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReportedByUserId",
                table: "ChoresLog",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_ChoresLog_ChoreUsers_ChoreUserId",
                table: "ChoresLog",
                column: "ChoreUserId",
                principalTable: "ChoreUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChoresLog_ChoreUsers_ChoreUserId",
                table: "ChoresLog");

            migrationBuilder.DropColumn(
                name: "ReportedByUserId",
                table: "ChoresLog");

            migrationBuilder.AlterColumn<bool>(
                name: "IsCompleted",
                table: "ChoresLog",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "ChoresLog",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ChoreUserId",
                table: "ChoresLog",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_ChoresLog_ChoreUsers_ChoreUserId",
                table: "ChoresLog",
                column: "ChoreUserId",
                principalTable: "ChoreUsers",
                principalColumn: "Id");
        }
    }
}
