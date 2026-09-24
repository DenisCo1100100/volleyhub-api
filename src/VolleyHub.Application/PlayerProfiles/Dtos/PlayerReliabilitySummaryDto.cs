namespace VolleyHub.Application.PlayerProfiles.Dtos
{
    public sealed record PlayerReliabilitySummaryDto(
        Guid PlayerProfileId,
        int AttendedGamesCount,
        int NoShowCount,
        int LateCancellationCount,
        int OnTimeCancellationCount,
        int UnmarkedGamesCount)
    {
        public int TotalMarkedGamesCount => AttendedGamesCount + NoShowCount;
        public decimal AttendanceRate => TotalMarkedGamesCount == 0
            ? 0
            : Math.Round((decimal)AttendedGamesCount / TotalMarkedGamesCount * 100, 2);
    }
}
