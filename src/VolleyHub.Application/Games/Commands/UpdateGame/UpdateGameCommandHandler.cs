using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.Games.Commands.UpdateGame
{
    public sealed class UpdateGameCommandHandler : IRequestHandler<UpdateGameCommand>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IGameParticipantRepository _gameParticipantRepository;
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateGameCommandHandler(
            IGameRepository gameRepository,
            IPlayerProfileRepository playerProfileRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            IGameParticipantRepository gameParticipantRepository)
        {
            _gameRepository = gameRepository;
            _gameParticipantRepository = gameParticipantRepository;
            _playerProfileRepository = playerProfileRepository;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            UpdateGameCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;

            if (currentUserId is null)
            {
                throw new UnauthorizedException();
            }

            var organizerProfile = await _playerProfileRepository.GetByUserIdAsync(
                currentUserId.Value,
                cancellationToken);

            if (organizerProfile is null || organizerProfile.IsDeleted)
            {
                throw new NotFoundException(nameof(PlayerProfile), currentUserId.Value);
            }

            var game = await _gameRepository.GetByIdAsync(
                request.Id,
                cancellationToken);

            if (game is null)
            {
                throw new NotFoundException(nameof(Game), request.Id);
            }

            if (game.OrganizerId != organizerProfile.Id)
            {
                throw new ForbiddenAccessException();
            }

            var participants = await _gameParticipantRepository.GetByGameIdAsync(game.Id, cancellationToken);
            var approvedCount = participants.Count(participant => participant.JoinStatus is GameParticipantJoinStatus.Approved);
            game.EnsureCapacity(approvedCount, request.MaxPlayers);
            game.EnsureCanChangePrice(request.PricePerPlayer, participants);
            var priceChanged = game.PricePerPlayer != request.PricePerPlayer;

            game.UpdateDetails(
                organizerProfile.Id,
                request.CourtId,
                request.StartsAt,
                request.EndsAt,
                request.MaxPlayers,
                request.PricePerPlayer,
                request.RequiredLevel,
                request.JoinPolicy,
                request.Description);

            if (priceChanged)
            {
                foreach (var participant in participants)
                {
                    if (game.PricePerPlayer == 0 || participant.JoinStatus is GameParticipantJoinStatus.Approved or GameParticipantJoinStatus.PendingApproval)
                    {
                        var trackedParticipant = await _gameParticipantRepository.GetByIdAsync(participant.Id, cancellationToken)
                            ?? throw new NotFoundException(nameof(GameParticipant), participant.Id);
                        var previousStatus = trackedParticipant.OfflinePaymentStatus;
                        trackedParticipant.SynchronizeOfflinePaymentRequirement(game);
                        if (trackedParticipant.OfflinePaymentStatus != previousStatus)
                        {
                            _gameParticipantRepository.Update(trackedParticipant);
                        }
                    }
                }
            }

            if (game.Status is GameStatus.Open && approvedCount == game.MaxPlayers)
            {
                game.MarkAsFull();
            }

            _gameRepository.Update(game);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
