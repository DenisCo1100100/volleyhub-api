using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VolleyHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGameParticipants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "game_participants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    join_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    attendance_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    offline_payment_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_participants", x => x.id);
                    table.ForeignKey(
                        name: "FK_game_participants_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_game_participants_game_id",
                table: "game_participants",
                column: "game_id");

            migrationBuilder.CreateIndex(
                name: "IX_game_participants_game_id_player_profile_id",
                table: "game_participants",
                columns: new[] { "game_id", "player_profile_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_participants_join_status",
                table: "game_participants",
                column: "join_status");

            migrationBuilder.CreateIndex(
                name: "IX_game_participants_player_profile_id",
                table: "game_participants",
                column: "player_profile_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_participants");
        }
    }
}
