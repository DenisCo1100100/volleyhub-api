using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VolleyHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGameTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "game_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    court_id = table.Column<Guid>(type: "uuid", nullable: false),
                    duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    max_players = table.Column<int>(type: "integer", nullable: false),
                    price_per_player = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    required_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    join_policy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_templates", x => x.id);
                    table.CheckConstraint("CK_game_templates_duration", "duration IS NULL OR duration > interval '0 seconds'");
                    table.ForeignKey(
                        name: "FK_game_templates_courts_court_id",
                        column: x => x.court_id,
                        principalTable: "courts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_game_templates_player_profiles_organizer_id",
                        column: x => x.organizer_id,
                        principalTable: "player_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_game_templates_court_id",
                table: "game_templates",
                column: "court_id");

            migrationBuilder.CreateIndex(
                name: "IX_game_templates_organizer_id",
                table: "game_templates",
                column: "organizer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_templates");
        }
    }
}
