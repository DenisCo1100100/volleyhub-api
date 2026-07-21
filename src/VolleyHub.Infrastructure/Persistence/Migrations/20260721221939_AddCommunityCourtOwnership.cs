using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VolleyHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityCourtOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "owner_player_profile_id",
                table: "courts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_courts_owner_player_profile_id",
                table: "courts",
                column: "owner_player_profile_id");

            migrationBuilder.AddForeignKey(
                name: "FK_courts_player_profiles_owner_player_profile_id",
                table: "courts",
                column: "owner_player_profile_id",
                principalTable: "player_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_courts_player_profiles_owner_player_profile_id",
                table: "courts");

            migrationBuilder.DropIndex(
                name: "IX_courts_owner_player_profile_id",
                table: "courts");

            migrationBuilder.DropColumn(
                name: "owner_player_profile_id",
                table: "courts");
        }
    }
}
