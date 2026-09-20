namespace VolleyHub.Application.Games.Common
{
    public sealed record GameOfflinePaymentSummaryDto(
        Guid GameId,
        decimal PricePerPlayer,
        int ExpectedParticipantCount,
        int PaidParticipantCount,
        int OutstandingParticipantCount)
    {
        public decimal ExpectedAmount => PricePerPlayer * ExpectedParticipantCount;
        public decimal PaidAmount => PricePerPlayer * PaidParticipantCount;
        public decimal OutstandingAmount => PricePerPlayer * OutstandingParticipantCount;
    }
}
