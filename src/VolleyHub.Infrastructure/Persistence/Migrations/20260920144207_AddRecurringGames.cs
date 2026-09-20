using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VolleyHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringGames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "occurrence_number",
                table: "games",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "recurrence_id",
                table: "games",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "game_recurrences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    court_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    occurrence_count = table.Column<int>(type: "integer", nullable: false),
                    max_players = table.Column<int>(type: "integer", nullable: false),
                    price_per_player = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    required_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    join_policy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_recurrences", x => x.id);
                    table.CheckConstraint("CK_game_recurrences_occurrence_count", "occurrence_count BETWEEN 2 AND 52");
                    table.ForeignKey(
                        name: "FK_game_recurrences_courts_court_id",
                        column: x => x.court_id,
                        principalTable: "courts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_game_recurrences_games_source_game_id",
                        column: x => x.source_game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_game_recurrences_player_profiles_organizer_id",
                        column: x => x.organizer_id,
                        principalTable: "player_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_games_recurrence_id_occurrence_number",
                table: "games",
                columns: new[] { "recurrence_id", "occurrence_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_games_recurrence_occurrence",
                table: "games",
                sql: "(recurrence_id IS NULL AND occurrence_number IS NULL) OR (recurrence_id IS NOT NULL AND occurrence_number IS NOT NULL AND occurrence_number BETWEEN 1 AND 52)");

            migrationBuilder.CreateIndex(
                name: "IX_game_recurrences_court_id",
                table: "game_recurrences",
                column: "court_id");

            migrationBuilder.CreateIndex(
                name: "IX_game_recurrences_organizer_id",
                table: "game_recurrences",
                column: "organizer_id");

            migrationBuilder.CreateIndex(
                name: "IX_game_recurrences_source_game_id",
                table: "game_recurrences",
                column: "source_game_id");

            migrationBuilder.AddForeignKey(
                name: "FK_games_game_recurrences_recurrence_id",
                table: "games",
                column: "recurrence_id",
                principalTable: "game_recurrences",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_games_game_recurrences_recurrence_id",
                table: "games");

            migrationBuilder.DropTable(
                name: "game_recurrences");

            migrationBuilder.DropIndex(
                name: "IX_games_recurrence_id_occurrence_number",
                table: "games");

            migrationBuilder.DropCheckConstraint(
                name: "CK_games_recurrence_occurrence",
                table: "games");

            migrationBuilder.DropColumn(
                name: "occurrence_number",
                table: "games");

            migrationBuilder.DropColumn(
                name: "recurrence_id",
                table: "games");
        }
    }
}
