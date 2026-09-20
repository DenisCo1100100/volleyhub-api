using MediatR;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameRecurrences.Common;

namespace VolleyHub.Application.GameRecurrences.Queries.GetGameRecurrence
{
    public sealed class GetGameRecurrenceQueryHandler : IRequestHandler<GetGameRecurrenceQuery, GameRecurrenceDto>
    {
        private readonly IGameRecurrenceRepository _recurrences;
        private readonly IPlayerProfileRepository _profiles;
        private readonly ICurrentUserService _currentUser;

        public GetGameRecurrenceQueryHandler(IGameRecurrenceRepository recurrences, IPlayerProfileRepository profiles, ICurrentUserService currentUser)
        {
            _recurrences = recurrences;
            _profiles = profiles;
            _currentUser = currentUser;
        }

        public async Task<GameRecurrenceDto> Handle(GetGameRecurrenceQuery request, CancellationToken cancellationToken)
        {
            var recurrence = await GameRecurrenceAccess.GetOwnedAsync(request.Id, _recurrences, _currentUser, _profiles, cancellationToken);
            var games = await _recurrences.GetOccurrencesAsync(recurrence.Id, cancellationToken);
            return new GameRecurrenceDto(recurrence.Id, recurrence.SourceGameId, recurrence.OrganizerId, recurrence.CourtId,
                recurrence.FirstStartsAt, recurrence.Duration, recurrence.OccurrenceCount, recurrence.MaxPlayers, recurrence.PricePerPlayer,
                recurrence.RequiredLevel, recurrence.JoinPolicy, recurrence.Description, recurrence.CancelledAt,
                games.OrderBy(game => game.OccurrenceNumber).Select(game => new GameOccurrenceDto(game.Id, game.OccurrenceNumber!.Value,
                    recurrence.GetStartsAt(game.OccurrenceNumber.Value), game.StartsAt, game.EndsAt, game.Status)).ToArray());
        }
    }
}
