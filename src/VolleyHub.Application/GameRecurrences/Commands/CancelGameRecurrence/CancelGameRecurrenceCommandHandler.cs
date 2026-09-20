using MediatR;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameRecurrences.Common;

namespace VolleyHub.Application.GameRecurrences.Commands.CancelGameRecurrence
{
    public sealed class CancelGameRecurrenceCommandHandler : IRequestHandler<CancelGameRecurrenceCommand>
    {
        private readonly IGameRecurrenceRepository _recurrences;
        private readonly IPlayerProfileRepository _profiles;
        private readonly ICurrentUserService _currentUser;
        private readonly IGameRepository _games;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _clock;

        public CancelGameRecurrenceCommandHandler(IGameRecurrenceRepository recurrences, IPlayerProfileRepository profiles,
            ICurrentUserService currentUser, IGameRepository games, IUnitOfWork unitOfWork, IDateTimeProvider clock)
        {
            _recurrences = recurrences;
            _profiles = profiles;
            _currentUser = currentUser;
            _games = games;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task Handle(CancelGameRecurrenceCommand request, CancellationToken cancellationToken)
        {
            var recurrence = await GameRecurrenceAccess.GetOwnedAsync(request.Id, _recurrences, _currentUser, _profiles, cancellationToken);
            if (recurrence.CancelledAt is not null)
            {
                return;
            }

            var now = _clock.UtcNow;
            var occurrences = await _recurrences.GetOccurrencesAsync(recurrence.Id, cancellationToken);
            foreach (var game in recurrence.SelectFutureOccurrences(occurrences, 1, now))
            {
                game.Cancel();
                _games.Update(game);
            }

            recurrence.Cancel(now);
            _recurrences.Update(recurrence);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
