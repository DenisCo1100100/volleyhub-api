using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VolleyHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandGameMvpSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ends_at",
                table: "games",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "join_policy",
                table: "games",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Open");

            migrationBuilder.AddColumn<Guid>(
                name: "organizer_id",
                table: "games",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "price_per_player",
                table: "games",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "required_level",
                table: "games",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Any");

            migrationBuilder.Sql("""
                UPDATE games
                SET status = 'Open'
                WHERE status = 'Scheduled';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_games_join_policy",
                table: "games",
                column: "join_policy");

            migrationBuilder.CreateIndex(
                name: "IX_games_organizer_id",
                table: "games",
                column: "organizer_id");

            migrationBuilder.CreateIndex(
                name: "IX_games_required_level",
                table: "games",
                column: "required_level");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_games_join_policy",
                table: "games");

            migrationBuilder.DropIndex(
                name: "IX_games_organizer_id",
                table: "games");

            migrationBuilder.DropIndex(
                name: "IX_games_required_level",
                table: "games");

            migrationBuilder.DropColumn(
                name: "ends_at",
                table: "games");

            migrationBuilder.DropColumn(
                name: "join_policy",
                table: "games");

            migrationBuilder.DropColumn(
                name: "organizer_id",
                table: "games");

            migrationBuilder.DropColumn(
                name: "price_per_player",
                table: "games");

            migrationBuilder.DropColumn(
                name: "required_level",
                table: "games");
        }
    }
}
