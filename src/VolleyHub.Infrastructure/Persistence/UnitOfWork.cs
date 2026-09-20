using VolleyHub.Application.Common.Interfaces;

using Microsoft.EntityFrameworkCore;
using Npgsql;
using VolleyHub.Application.Common.Exceptions;

namespace VolleyHub.Infrastructure.Persistence
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("The game, recurrence or participation changed. Refresh and try again.");
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_game_participants_game_id_player_profile_id" })
            {
                throw new ConflictException("Player has already joined this game.");
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "PK_game_recurrences" or "IX_games_recurrence_id_occurrence_number" })
            {
                throw new ConflictException("The recurrence or occurrence already exists. Read the existing recurrence before retrying.");
            }
        }
    }
}
