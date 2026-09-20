using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Games.Common;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.Games.Queries.GetGameOfflinePaymentSummary
{
    public sealed class GetGameOfflinePaymentSummaryQueryHandler : IRequestHandler<GetGameOfflinePaymentSummaryQuery, GameOfflinePaymentSummaryDto>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IGameParticipantRepository _gameParticipantRepository;
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly ICurrentUserService _currentUserService;

        public GetGameOfflinePaymentSummaryQueryHandler(IGameRepository gameRepository, IGameParticipantRepository gameParticipantRepository,
            IPlayerProfileRepository playerProfileRepository, ICurrentUserService currentUserService)
        {
            _gameRepository = gameRepository;
            _gameParticipantRepository = gameParticipantRepository;
            _playerProfileRepository = playerProfileRepository;
            _currentUserService = currentUserService;
        }

        public async Task<GameOfflinePaymentSummaryDto> Handle(GetGameOfflinePaymentSummaryQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId ?? throw new UnauthorizedException();
            var organizer = await _playerProfileRepository.GetByUserIdAsync(currentUserId, cancellationToken);
            if (organizer is null || organizer.IsDeleted)
            {
                throw new NotFoundException(nameof(PlayerProfile), currentUserId);
            }

            var game = await _gameRepository.GetByIdAsync(request.GameId, cancellationToken)
                ?? throw new NotFoundException(nameof(Game), request.GameId);
            if (game.OrganizerId != organizer.Id)
            {
                throw new ForbiddenAccessException();
            }

            var participants = await _gameParticipantRepository.GetByGameIdAsync(game.Id, cancellationToken);
            var expected = game.PricePerPlayer > 0 && game.Status is GameStatus.Open or GameStatus.Full or GameStatus.Completed
                ? participants.Where(participant => participant.JoinStatus is GameParticipantJoinStatus.Approved).ToArray()
                : [];
            var paidCount = expected.Count(participant => participant.OfflinePaymentStatus is GameParticipantOfflinePaymentStatus.Paid);

            return new GameOfflinePaymentSummaryDto(game.Id, game.PricePerPlayer, expected.Length, paidCount, expected.Length - paidCount);
        }
    }
}
