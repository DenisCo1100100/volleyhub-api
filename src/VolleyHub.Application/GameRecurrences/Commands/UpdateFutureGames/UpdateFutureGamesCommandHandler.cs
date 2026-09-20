using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameRecurrences.Common;
using VolleyHub.Application.Games.Commands.UpdateGame;
using VolleyHub.Application.Games.Common;
using VolleyHub.Domain.Common;
using VolleyHub.Domain.Courts;

namespace VolleyHub.Application.GameRecurrences.Commands.UpdateFutureGames
{
    public sealed class UpdateFutureGamesCommandHandler : IRequestHandler<UpdateFutureGamesCommand>
    {
        private readonly IGameRecurrenceRepository _recurrences;
        private readonly IPlayerProfileRepository _profiles;
        private readonly ICurrentUserService _currentUser;
        private readonly IGameRepository _games;
        private readonly IGameParticipantRepository _participants;
        private readonly ICourtRepository _courts;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _clock;

        public UpdateFutureGamesCommandHandler(IGameRecurrenceRepository recurrences, IPlayerProfileRepository profiles,
            ICurrentUserService currentUser, IGameRepository games, IGameParticipantRepository participants,
            ICourtRepository courts, IUnitOfWork unitOfWork, IDateTimeProvider clock)
        {
            _recurrences = recurrences;
            _profiles = profiles;
            _currentUser = currentUser;
            _games = games;
            _participants = participants;
            _courts = courts;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task Handle(UpdateFutureGamesCommand request, CancellationToken cancellationToken)
        {
            var recurrence = await GameRecurrenceAccess.GetOwnedAsync(request.Id, _recurrences, _currentUser, _profiles, cancellationToken);
            var court = await _courts.GetByIdAsync(request.CourtId, cancellationToken);
            if (court is null || court.IsDeleted)
            {
                throw new NotFoundException(nameof(Court), request.CourtId);
            }

            var occurrences = await _recurrences.GetOccurrencesAsync(recurrence.Id, cancellationToken);
            var future = recurrence.SelectFutureOccurrences(occurrences, request.FromOccurrenceNumber, _clock.UtcNow);
            if (future.Count == 0)
            {
                throw new BusinessRuleException("There are no future editable occurrences in this range.");
            }

            foreach (var game in future)
            {
                var update = new UpdateGameCommand(game.Id, request.CourtId, game.StartsAt, game.EndsAt,
                    request.MaxPlayers, request.PricePerPlayer, request.RequiredLevel, request.JoinPolicy, request.Description);
                await GameDetailsUpdater.ApplyAsync(game, update, _participants, cancellationToken);
                _games.Update(game);
            }

            recurrence.UpdateSettingsFrom(future[0]);
            _recurrences.Update(recurrence);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
