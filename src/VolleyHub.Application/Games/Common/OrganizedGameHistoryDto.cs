namespace VolleyHub.Application.Games.Common
{
    public sealed record OrganizedGameHistoryDto(
        GameSummaryDto Game,
        int WaitlistedParticipantCount,
        int PresentParticipantCount,
        int AbsentParticipantCount,
        int UnmarkedAttendanceParticipantCount,
        GameOfflinePaymentSummaryDto OfflinePayments);
}
