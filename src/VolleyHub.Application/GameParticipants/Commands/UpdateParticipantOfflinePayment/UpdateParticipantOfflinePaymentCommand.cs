using MediatR;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameParticipants.Commands.UpdateParticipantOfflinePayment
{
    public sealed record UpdateParticipantOfflinePaymentCommand(Guid ParticipantId, GameParticipantOfflinePaymentStatus OfflinePaymentStatus) : IRequest;
}
