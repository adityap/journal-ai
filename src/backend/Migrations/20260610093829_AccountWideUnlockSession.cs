using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JournalAI.Migrations
{
    /// <inheritdoc />
    public partial class AccountWideUnlockSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnlockSessions_Entries_EntryId",
                table: "UnlockSessions");

            migrationBuilder.AlterColumn<Guid>(
                name: "EntryId",
                table: "UnlockSessions",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddForeignKey(
                name: "FK_UnlockSessions_Entries_EntryId",
                table: "UnlockSessions",
                column: "EntryId",
                principalTable: "Entries",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnlockSessions_Entries_EntryId",
                table: "UnlockSessions");

            migrationBuilder.AlterColumn<Guid>(
                name: "EntryId",
                table: "UnlockSessions",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UnlockSessions_Entries_EntryId",
                table: "UnlockSessions",
                column: "EntryId",
                principalTable: "Entries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
