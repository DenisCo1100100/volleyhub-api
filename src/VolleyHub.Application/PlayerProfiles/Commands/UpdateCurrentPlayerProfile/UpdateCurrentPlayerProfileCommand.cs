using MediatR;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.PlayerProfiles.Commands.UpdateCurrentPlayerProfile
{
    public sealed record UpdateCurrentPlayerProfileCommand(
        string DisplayName,
        PlayerSkillLevel SkillLevel,
        string? City,
        string? Bio) : IRequest;
}