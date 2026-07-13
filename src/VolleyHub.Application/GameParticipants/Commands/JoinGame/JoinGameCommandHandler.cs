using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameParticipants.Commands.JoinGame
{
    public sealed class JoinGameCommandHandler : IRequestHandler<JoinGameCommand, Guid>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IGameParticipantRepository _gameParticipantRepository;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IUnitOfWork _unitOfWork;

        public JoinGameCommandHandler(
            IGameRepository gameRepository,
            IGameParticipantRepository gameParticipantRepository,
            IDateTimeProvider dateTimeProvider,
            IUnitOfWork unitOfWork)
        {
            _gameRepository = gameRepository;
            _gameParticipantRepository = gameParticipantRepository;
            _dateTimeProvider = dateTimeProvider;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(
            JoinGameCommand request,
            CancellationToken cancellationToken)
        {
            var game = await _gameRepository.GetByIdAsync(
                request.GameId,
                cancellationToken);

            if (game is null)
            {
                throw new NotFoundException(nameof(Game), request.GameId);
            }

            if (game.Status is not GameStatus.Open)
            {
                throw new InvalidOperationException("Only open games can be joined.");
            }

            if (game.JoinPolicy is GameJoinPolicy.InviteOnly)
            {
                throw new InvalidOperationException("Invite-only games cannot be joined directly.");
            }

            var existingParticipant = await _gameParticipantRepository.GetByGameAndPlayerProfileIdAsync(
                request.GameId,
                request.PlayerProfileId,
                cancellationToken);

            if (existingParticipant is not null)
            {
                throw new InvalidOperationException("Player has already joined this game.");
            }

            var participants = await _gameParticipantRepository.GetByGameIdAsync(
                request.GameId,
                cancellationToken);

            var approvedParticipantsCount = participants.Count(
                participant => participant.JoinStatus is GameParticipantJoinStatus.Approved);

            var offlinePaymentStatus = game.PricePerPlayer > 0
                ? GameParticipantOfflinePaymentStatus.Pending
                : GameParticipantOfflinePaymentStatus.NotRequired;

            var now = _dateTimeProvider.UtcNow;

            var participant = game.JoinPolicy switch
            {
                GameJoinPolicy.Open => JoinOpenGame(
                    game,
                    request.PlayerProfileId,
                    now,
                    offlinePaymentStatus,
                    approvedParticipantsCount),

                GameJoinPolicy.ApprovalRequired => GameParticipant.RequestToJoin(
                    game.Id,
                    request.PlayerProfileId,
                    now,
                    offlinePaymentStatus),

                _ => throw new InvalidOperationException("Game join policy is invalid.")
            };

            await _gameParticipantRepository.AddAsync(participant, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return participant.Id;
        }

        private GameParticipant JoinOpenGame(
            Game game,
            Guid playerProfileId,
            DateTimeOffset joinedAt,
            GameParticipantOfflinePaymentStatus offlinePaymentStatus,
            int approvedParticipantsCount)
        {
            if (approvedParticipantsCount >= game.MaxPlayers)
            {
                throw new InvalidOperationException("Game is full.");
            }

            var participant = GameParticipant.JoinOpenGame(
                game.Id,
                playerProfileId,
                joinedAt,
                offlinePaymentStatus);

            if (approvedParticipantsCount + 1 >= game.MaxPlayers)
            {
                game.MarkAsFull();
                _gameRepository.Update(game);
            }

            return participant;
        }
    }
}