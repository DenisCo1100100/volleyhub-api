using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.GameRecurrences.Common
{
    internal static class GameRecurrenceAccess
    {
        public static async Task<PlayerProfile> GetOrganizerAsync(ICurrentUserService currentUser, IPlayerProfileRepository profiles, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? throw new UnauthorizedException();
            var profile = await profiles.GetByUserIdAsync(userId, cancellationToken);
            if (profile is null || profile.IsDeleted)
            {
                throw new NotFoundException(nameof(PlayerProfile), userId);
            }

            return profile;
        }

        public static async Task<GameRecurrence> GetOwnedAsync(Guid id, IGameRecurrenceRepository recurrences,
            ICurrentUserService currentUser, IPlayerProfileRepository profiles, CancellationToken cancellationToken)
        {
            var organizer = await GetOrganizerAsync(currentUser, profiles, cancellationToken);
            var recurrence = await recurrences.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException(nameof(GameRecurrence), id);
            if (recurrence.OrganizerId != organizer.Id)
            {
                throw new ForbiddenAccessException();
            }

            return recurrence;
        }
    }
}
