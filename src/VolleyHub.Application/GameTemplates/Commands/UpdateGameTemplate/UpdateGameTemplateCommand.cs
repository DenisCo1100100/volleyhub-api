using MediatR;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameTemplates.Commands.UpdateGameTemplate
{
    public sealed record UpdateGameTemplateCommand(Guid Id, string Name, Guid CourtId, TimeSpan? Duration, int MaxPlayers,
        decimal PricePerPlayer, GameLevel RequiredLevel, GameJoinPolicy JoinPolicy, string? Description) : IRequest;
}
