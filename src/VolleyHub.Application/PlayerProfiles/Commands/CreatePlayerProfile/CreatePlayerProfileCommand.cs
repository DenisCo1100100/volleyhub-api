using MediatR;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.PlayerProfiles.Commands.CreatePlayerProfile
{
    public sealed record CreatePlayerProfileCommand(
        string DisplayName,
        PlayerSkillLevel SkillLevel,
        string? City,
        string? Bio) : IRequest<Guid>;
}