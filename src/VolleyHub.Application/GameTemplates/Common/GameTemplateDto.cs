using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameTemplates.Common
{
    public sealed record GameTemplateDto(Guid Id, string Name, Guid CourtId, TimeSpan? Duration, int MaxPlayers,
        decimal PricePerPlayer, GameLevel RequiredLevel, GameJoinPolicy JoinPolicy, string? Description,
        DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);

    internal static class GameTemplateMappingExtensions
    {
        public static GameTemplateDto ToDto(this GameTemplate template) => new(template.Id, template.Name, template.CourtId,
            template.Duration, template.MaxPlayers, template.PricePerPlayer, template.RequiredLevel, template.JoinPolicy,
            template.Description, template.CreatedAt, template.UpdatedAt);
    }
}
