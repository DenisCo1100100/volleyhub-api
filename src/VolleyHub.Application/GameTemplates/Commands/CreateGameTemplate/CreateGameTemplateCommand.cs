using MediatR;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameTemplates.Commands.CreateGameTemplate
{
    public sealed record CreateGameTemplateCommand(string Name, Guid CourtId, TimeSpan? Duration, int MaxPlayers,
        decimal PricePerPlayer, GameLevel RequiredLevel, GameJoinPolicy JoinPolicy, string? Description) : IRequest<Guid>;
}
