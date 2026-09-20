using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VolleyHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOfflinePaymentTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Earlier game price edits did not synchronize the existing payment status.
            migrationBuilder.Sql("""
                UPDATE game_participants AS participant
                SET offline_payment_status = 'NotRequired'
                FROM games AS game
                WHERE participant.game_id = game.id
                  AND (game.price_per_player = 0 OR participant.join_status = 'Waitlisted')
                  AND participant.offline_payment_status <> 'NotRequired';

                UPDATE game_participants AS participant
                SET offline_payment_status = 'Pending'
                FROM games AS game
                WHERE participant.game_id = game.id
                  AND game.price_per_player > 0
                  AND participant.join_status IN ('Approved', 'PendingApproval')
                  AND participant.offline_payment_status = 'NotRequired';
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Normalization cannot reconstruct earlier inconsistent payment states.

        }
    }
}
