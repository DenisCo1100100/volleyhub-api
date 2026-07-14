namespace VolleyHub.Application.PlayerProfiles.Dtos
{
    public sealed record PlayerReliabilitySummaryDto(
        Guid PlayerProfileId,
        int AttendedGamesCount,
        int NoShowCount,
        int LateCancellationCount,
        int TotalMarkedGamesCount,
        decimal AttendanceRate);
}