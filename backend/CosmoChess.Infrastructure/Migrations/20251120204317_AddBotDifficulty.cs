using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CosmoChess.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBotDifficulty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BotDifficulty",
                table: "games",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BotDifficulty",
                table: "games");
        }
    }
}
