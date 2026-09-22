using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.GameTemplates.Common
{
    internal static class GameTemplateAccess
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

        public static async Task<GameTemplate> GetOwnedAsync(Guid id, IGameTemplateRepository templates,
            ICurrentUserService currentUser, IPlayerProfileRepository profiles, CancellationToken cancellationToken)
        {
            var organizer = await GetOrganizerAsync(currentUser, profiles, cancellationToken);
            var template = await templates.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException(nameof(GameTemplate), id);
            if (template.OrganizerId != organizer.Id)
            {
                throw new ForbiddenAccessException();
            }

            return template;
        }

        public static async Task EnsureCourtExistsAsync(Guid courtId, ICourtRepository courts, CancellationToken cancellationToken)
        {
            var court = await courts.GetByIdAsync(courtId, cancellationToken);
            if (court is null || court.IsDeleted)
            {
                throw new NotFoundException(nameof(Court), courtId);
            }
        }
    }
}
