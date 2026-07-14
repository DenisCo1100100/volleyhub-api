using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VolleyHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGameParticipantPlayerProfileRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_game_participants_player_profiles_player_profile_id",
                table: "game_participants",
                column: "player_profile_id",
                principalTable: "player_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_game_participants_player_profiles_player_profile_id",
                table: "game_participants");
        }
    }
}
