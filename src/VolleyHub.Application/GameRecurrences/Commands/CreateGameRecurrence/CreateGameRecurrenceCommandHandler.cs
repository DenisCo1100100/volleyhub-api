using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameRecurrences.Common;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameRecurrences.Commands.CreateGameRecurrence
{
    public sealed class CreateGameRecurrenceCommandHandler : IRequestHandler<CreateGameRecurrenceCommand, Guid>
    {
        private readonly IGameRecurrenceRepository _recurrences;
        private readonly IPlayerProfileRepository _profiles;
        private readonly ICurrentUserService _currentUser;
        private readonly IGameRepository _games;
        private readonly ICourtRepository _courts;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _clock;

        public CreateGameRecurrenceCommandHandler(IGameRecurrenceRepository recurrences, IPlayerProfileRepository profiles,
            ICurrentUserService currentUser, IGameRepository games, ICourtRepository courts, IUnitOfWork unitOfWork, IDateTimeProvider clock)
        {
            _recurrences = recurrences;
            _profiles = profiles;
            _currentUser = currentUser;
            _games = games;
            _courts = courts;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<Guid> Handle(CreateGameRecurrenceCommand request, CancellationToken cancellationToken)
        {
            var organizer = await GameRecurrenceAccess.GetOrganizerAsync(_currentUser, _profiles, cancellationToken);
            var source = await _games.GetByIdAsync(request.SourceGameId, cancellationToken)
                ?? throw new NotFoundException(nameof(Game), request.SourceGameId);
            if (source.OrganizerId != organizer.Id)
            {
                throw new ForbiddenAccessException();
            }

            var court = await _courts.GetByIdAsync(source.CourtId, cancellationToken);
            if (court is null || court.IsDeleted)
            {
                throw new NotFoundException(nameof(Court), source.CourtId);
            }

            if (await _recurrences.GetByIdAsync(request.Id, cancellationToken) is not null)
            {
                throw new ConflictException("This recurrence id already exists. Read the existing recurrence before retrying.");
            }

            var recurrence = GameRecurrence.Create(request.Id, source, request.FirstStartsAt, request.OccurrenceCount, _clock.UtcNow);
            await _recurrences.AddAsync(recurrence, cancellationToken);
            for (var number = 1; number <= recurrence.OccurrenceCount; number++)
            {
                await _games.AddAsync(recurrence.CreateOccurrence(number), cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return recurrence.Id;
        }
    }
}
