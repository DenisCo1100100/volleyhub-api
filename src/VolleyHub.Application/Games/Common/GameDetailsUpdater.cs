using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Games.Commands.UpdateGame;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Games.Common
{
    internal static class GameDetailsUpdater
    {
        public static async Task ApplyAsync(Game game, UpdateGameCommand request, IGameParticipantRepository participantsRepository, CancellationToken cancellationToken)
        {
            var participants = await participantsRepository.GetByGameIdAsync(game.Id, cancellationToken);
            var approvedCount = participants.Count(participant => participant.JoinStatus is GameParticipantJoinStatus.Approved);
            game.EnsureCapacity(approvedCount, request.MaxPlayers);
            game.EnsureCanChangePrice(request.PricePerPlayer, participants);
            var priceChanged = game.PricePerPlayer != request.PricePerPlayer;

            game.UpdateDetails(
                game.OrganizerId,
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
                        var trackedParticipant = await participantsRepository.GetByIdAsync(participant.Id, cancellationToken)
                            ?? throw new NotFoundException(nameof(GameParticipant), participant.Id);
                        var previousStatus = trackedParticipant.OfflinePaymentStatus;
                        trackedParticipant.SynchronizeOfflinePaymentRequirement(game);
                        if (trackedParticipant.OfflinePaymentStatus != previousStatus)
                        {
                            participantsRepository.Update(trackedParticipant);
                        }
                    }
                }
            }

            if (game.Status is GameStatus.Open && approvedCount == game.MaxPlayers)
            {
                game.MarkAsFull();
            }
        }
    }
}
