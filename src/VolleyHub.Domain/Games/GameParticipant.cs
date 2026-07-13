using VolleyHub.Domain.Common;

namespace VolleyHub.Domain.Games
{
    public sealed class GameParticipant : AuditableEntity
    {
        private GameParticipant() { }

        private GameParticipant(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; private set; }
        public Guid GameId { get; private set; }
        public Guid PlayerProfileId { get; private set; }
        public DateTimeOffset JoinedAt { get; private set; }
        public DateTimeOffset? ApprovedAt { get; private set; }
        public GameParticipantJoinStatus JoinStatus { get; private set; }
        public GameParticipantAttendanceStatus AttendanceStatus { get; private set; }
        public GameParticipantOfflinePaymentStatus OfflinePaymentStatus { get; private set; }

        public static GameParticipant RequestToJoin(
            Guid gameId,
            Guid playerProfileId,
            DateTimeOffset joinedAt,
            GameParticipantOfflinePaymentStatus offlinePaymentStatus)
        {
            ValidateGameId(gameId);
            ValidatePlayerProfileId(playerProfileId);
            ValidateJoinedAt(joinedAt);
            ValidateOfflinePaymentStatus(offlinePaymentStatus);

            return new GameParticipant(Guid.NewGuid())
            {
                GameId = gameId,
                PlayerProfileId = playerProfileId,
                JoinedAt = joinedAt,
                JoinStatus = GameParticipantJoinStatus.PendingApproval,
                AttendanceStatus = GameParticipantAttendanceStatus.NotMarked,
                OfflinePaymentStatus = offlinePaymentStatus
            };
        }

        public static GameParticipant JoinOpenGame(
            Guid gameId,
            Guid playerProfileId,
            DateTimeOffset joinedAt,
            GameParticipantOfflinePaymentStatus offlinePaymentStatus)
        {
            ValidateGameId(gameId);
            ValidatePlayerProfileId(playerProfileId);
            ValidateJoinedAt(joinedAt);
            ValidateOfflinePaymentStatus(offlinePaymentStatus);

            return new GameParticipant(Guid.NewGuid())
            {
                GameId = gameId,
                PlayerProfileId = playerProfileId,
                JoinedAt = joinedAt,
                ApprovedAt = joinedAt,
                JoinStatus = GameParticipantJoinStatus.Approved,
                AttendanceStatus = GameParticipantAttendanceStatus.NotMarked,
                OfflinePaymentStatus = offlinePaymentStatus
            };
        }

        public void Approve(DateTimeOffset approvedAt)
        {
            if (JoinStatus is not GameParticipantJoinStatus.PendingApproval)
            {
                throw new InvalidOperationException("Only pending join requests can be approved.");
            }

            ValidateApprovedAt(approvedAt);

            JoinStatus = GameParticipantJoinStatus.Approved;
            ApprovedAt = approvedAt;
        }

        public void Reject()
        {
            if (JoinStatus is not GameParticipantJoinStatus.PendingApproval)
            {
                throw new InvalidOperationException("Only pending join requests can be rejected.");
            }

            JoinStatus = GameParticipantJoinStatus.Rejected;
        }

        public void Cancel()
        {
            if (JoinStatus is GameParticipantJoinStatus.Rejected)
            {
                throw new InvalidOperationException("Rejected join requests cannot be cancelled.");
            }

            if (JoinStatus is GameParticipantJoinStatus.Cancelled)
            {
                throw new InvalidOperationException("Join request is already cancelled.");
            }

            JoinStatus = GameParticipantJoinStatus.Cancelled;
        }

        public void MarkAttendance(GameParticipantAttendanceStatus attendanceStatus)
        {
            if (JoinStatus is not GameParticipantJoinStatus.Approved)
            {
                throw new InvalidOperationException("Only approved participants can have attendance marked.");
            }

            ValidateAttendanceStatus(attendanceStatus);

            AttendanceStatus = attendanceStatus;
        }

        public void MarkOfflinePaymentAsPaid()
        {
            if (OfflinePaymentStatus is GameParticipantOfflinePaymentStatus.NotRequired)
            {
                throw new InvalidOperationException("Payment is not required for this participant.");
            }

            OfflinePaymentStatus = GameParticipantOfflinePaymentStatus.Paid;
        }

        private static void ValidateGameId(Guid gameId)
        {
            if (gameId == Guid.Empty)
            {
                throw new ArgumentException("Game id is required.", nameof(gameId));
            }
        }

        private static void ValidatePlayerProfileId(Guid playerProfileId)
        {
            if (playerProfileId == Guid.Empty)
            {
                throw new ArgumentException("Player profile id is required.", nameof(playerProfileId));
            }
        }

        private static void ValidateJoinedAt(DateTimeOffset joinedAt)
        {
            if (joinedAt == default)
            {
                throw new ArgumentException("Joined date and time is required.", nameof(joinedAt));
            }
        }

        private void ValidateApprovedAt(DateTimeOffset approvedAt)
        {
            if (approvedAt == default)
            {
                throw new ArgumentException("Approved date and time is required.", nameof(approvedAt));
            }

            if (approvedAt < JoinedAt)
            {
                throw new ArgumentException("Approved date and time cannot be before joined date and time.", nameof(approvedAt));
            }
        }

        private static void ValidateAttendanceStatus(GameParticipantAttendanceStatus attendanceStatus)
        {
            if (attendanceStatus is GameParticipantAttendanceStatus.Unknown
                or GameParticipantAttendanceStatus.NotMarked
                || !Enum.IsDefined(attendanceStatus))
            {
                throw new ArgumentException("Attendance status is invalid.", nameof(attendanceStatus));
            }
        }

        private static void ValidateOfflinePaymentStatus(GameParticipantOfflinePaymentStatus offlinePaymentStatus)
        {
            if (offlinePaymentStatus is GameParticipantOfflinePaymentStatus.Unknown
                || !Enum.IsDefined(offlinePaymentStatus))
            {
                throw new ArgumentException("Offline payment status is invalid.", nameof(offlinePaymentStatus));
            }
        }
    }
}