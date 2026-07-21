using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VolleyHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGameCourtAndOrganizerRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_games_courts_court_id",
                table: "games",
                column: "court_id",
                principalTable: "courts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_games_player_profiles_organizer_id",
                table: "games",
                column: "organizer_id",
                principalTable: "player_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_games_courts_court_id",
                table: "games");

            migrationBuilder.DropForeignKey(
                name: "FK_games_player_profiles_organizer_id",
                table: "games");
        }
    }
}
