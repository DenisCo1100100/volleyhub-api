namespace VolleyHub.Domain.Games
{
    public enum GameParticipantJoinStatus
    {
        Unknown = 0,
        PendingApproval = 1,
        Approved = 2,
        Rejected = 3,
        Cancelled = 4,
        Removed = 5,
        Waitlisted = 6
    }
}
