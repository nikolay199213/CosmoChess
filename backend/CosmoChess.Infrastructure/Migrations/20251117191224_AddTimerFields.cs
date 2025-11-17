using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CosmoChess.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTimerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BlackTimeRemainingSeconds",
                table: "games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMoveTime",
                table: "games",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeControl",
                table: "games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WhiteTimeRemainingSeconds",
                table: "games",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlackTimeRemainingSeconds",
                table: "games");

            migrationBuilder.DropColumn(
                name: "LastMoveTime",
                table: "games");

            migrationBuilder.DropColumn(
                name: "TimeControl",
                table: "games");

            migrationBuilder.DropColumn(
                name: "WhiteTimeRemainingSeconds",
                table: "games");
        }
    }
}
