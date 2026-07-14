using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VolleyHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    skill_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bio = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_player_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_profiles_city",
                table: "player_profiles",
                column: "city");

            migrationBuilder.CreateIndex(
                name: "IX_player_profiles_display_name",
                table: "player_profiles",
                column: "display_name");

            migrationBuilder.CreateIndex(
                name: "IX_player_profiles_skill_level",
                table: "player_profiles",
                column: "skill_level");

            migrationBuilder.CreateIndex(
                name: "IX_player_profiles_user_id",
                table: "player_profiles",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_profiles");
        }
    }
}
