using VolleyHub.Application.Games.Dtos;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Games.Mappings
{
    public static class GameMappingExtensions
    {
        public static GameDto ToDto(this Game game)
        {
            return new GameDto(
                game.Id,
                game.OrganizerId,
                game.CourtId,
                game.StartsAt,
                game.EndsAt,
                game.MaxPlayers,
                game.PricePerPlayer,
                game.RequiredLevel,
                game.JoinPolicy,
                game.Description,
                game.Status,
                game.CreatedAt,
                game.UpdatedAt);
        }
    }
}