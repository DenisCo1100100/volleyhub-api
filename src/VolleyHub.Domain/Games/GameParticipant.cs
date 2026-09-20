using VolleyHub.Domain.Common;

namespace VolleyHub.Domain.Games
{
    public sealed class GameParticipant : AuditableEntity
    {
        public const int LateCancellationThresholdHours = 24;

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
        public DateTimeOffset? CancelledAt { get; private set; }
        public DateTimeOffset? RemovedAt { get; private set; }
        public GameParticipantJoinStatus JoinStatus { get; private set; }
        public GameParticipantAttendanceStatus AttendanceStatus { get; private set; }
        public GameParticipantOfflinePaymentStatus OfflinePaymentStatus { get; private set; }
        public GameParticipantCancellationType? CancellationType { get; private set; }

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

        public static GameParticipant JoinWaitlist(Game game, Guid playerProfileId, DateTimeOffset joinedAt, int approvedParticipantCount)
        {
            game.EnsureCanJoinWaitlist(approvedParticipantCount, joinedAt);

            var participant = RequestToJoin(game.Id, playerProfileId, joinedAt, GameParticipantOfflinePaymentStatus.NotRequired);
            participant.JoinStatus = GameParticipantJoinStatus.Waitlisted;
            return participant;
        }

        public void PromoteFromWaitlist(Game game, int approvedParticipantCount, DateTimeOffset promotedAt)
        {
            if (JoinStatus is not GameParticipantJoinStatus.Waitlisted || GameId != game.Id)
            {
                throw new BusinessRuleException("Only waitlisted participants of this game can be promoted.");
            }

            ValidateApprovedAt(promotedAt);
            game.EnsureCanPromoteFromWaitlist(approvedParticipantCount, promotedAt);

            JoinStatus = GameParticipantJoinStatus.Approved;
            ApprovedAt = promotedAt;
            OfflinePaymentStatus = game.PricePerPlayer > 0
                ? GameParticipantOfflinePaymentStatus.Pending
                : GameParticipantOfflinePaymentStatus.NotRequired;

            if (approvedParticipantCount + 1 == game.MaxPlayers)
            {
                game.MarkAsFull();
            }
        }

        public void WithdrawFromWaitlist()
        {
            EnsureIsWaitlisted();
            JoinStatus = GameParticipantJoinStatus.Cancelled;
        }

        public void RejectFromWaitlist()
        {
            EnsureIsWaitlisted();
            JoinStatus = GameParticipantJoinStatus.Rejected;
        }

        private void EnsureIsWaitlisted()
        {
            if (JoinStatus is not GameParticipantJoinStatus.Waitlisted)
            {
                throw new BusinessRuleException("Participant is not on the waitlist.");
            }
        }

        public void Approve(DateTimeOffset approvedAt)
        {
            if (JoinStatus is not GameParticipantJoinStatus.PendingApproval)
            {
                throw new BusinessRuleException("Only pending join requests can be approved.");
            }

            ValidateApprovedAt(approvedAt);

            JoinStatus = GameParticipantJoinStatus.Approved;
            ApprovedAt = approvedAt;
        }

        public void Reject()
        {
            if (JoinStatus is not GameParticipantJoinStatus.PendingApproval)
            {
                throw new BusinessRuleException("Only pending join requests can be rejected.");
            }

            JoinStatus = GameParticipantJoinStatus.Rejected;
        }

        public void WithdrawRequest()
        {
            if (JoinStatus is not GameParticipantJoinStatus.PendingApproval)
            {
                throw new BusinessRuleException("Only pending join requests can be withdrawn.");
            }

            JoinStatus = GameParticipantJoinStatus.Cancelled;
        }

        public void CancelParticipation(
            DateTimeOffset cancelledAt,
            DateTimeOffset gameStartsAt)
        {
            if (JoinStatus is not GameParticipantJoinStatus.Approved)
            {
                throw new BusinessRuleException("Only approved participants can cancel participation.");
            }

            ValidateGameStartsAt(gameStartsAt);
            ValidateParticipationEventTime(cancelledAt, nameof(cancelledAt));

            if (cancelledAt >= gameStartsAt)
            {
                throw new BusinessRuleException("Participation cannot be cancelled after the game has started.");
            }

            var lateCancellationThreshold =
                gameStartsAt.AddHours(-LateCancellationThresholdHours);

            CancellationType = cancelledAt < lateCancellationThreshold
                ? GameParticipantCancellationType.OnTime
                : GameParticipantCancellationType.Late;

            CancelledAt = cancelledAt;
            JoinStatus = GameParticipantJoinStatus.Cancelled;
        }

        public void Remove(
            DateTimeOffset removedAt,
            DateTimeOffset gameStartsAt)
        {
            if (JoinStatus is not GameParticipantJoinStatus.Approved)
            {
                throw new BusinessRuleException("Only approved participants can be removed.");
            }

            ValidateGameStartsAt(gameStartsAt);
            ValidateParticipationEventTime(removedAt, nameof(removedAt));

            if (removedAt >= gameStartsAt)
            {
                throw new BusinessRuleException("Participants cannot be removed after the game has started.");
            }

            RemovedAt = removedAt;
            JoinStatus = GameParticipantJoinStatus.Removed;
        }

        public void MarkAttendance(GameParticipantAttendanceStatus attendanceStatus)
        {
            if (JoinStatus is not GameParticipantJoinStatus.Approved)
            {
                throw new BusinessRuleException("Only approved participants can have attendance marked.");
            }

            ValidateAttendanceStatus(attendanceStatus);

            AttendanceStatus = attendanceStatus;
        }

        public void UpdateOfflinePaymentStatus(Game game, GameParticipantOfflinePaymentStatus offlinePaymentStatus)
        {
            EnsureBelongsToGame(game);
            ValidateOfflinePaymentStatus(offlinePaymentStatus);

            if (JoinStatus is not GameParticipantJoinStatus.Approved)
            {
                throw new BusinessRuleException("Only approved participants can have their payment status updated.");
            }

            if (game.Status is not GameStatus.Open and not GameStatus.Full and not GameStatus.Completed)
            {
                throw new BusinessRuleException("Payment status can be updated only for open, full, or completed games.");
            }

            if (game.PricePerPlayer == 0 && offlinePaymentStatus is not GameParticipantOfflinePaymentStatus.NotRequired)
            {
                throw new BusinessRuleException("Free games do not require payment.");
            }

            if (game.PricePerPlayer > 0 && offlinePaymentStatus is GameParticipantOfflinePaymentStatus.NotRequired)
            {
                throw new BusinessRuleException("Paid games require a pending or paid payment status.");
            }

            OfflinePaymentStatus = offlinePaymentStatus;
        }

        public void SynchronizeOfflinePaymentRequirement(Game game)
        {
            EnsureBelongsToGame(game);

            if ((game.PricePerPlayer == 0 || JoinStatus is GameParticipantJoinStatus.Approved or GameParticipantJoinStatus.PendingApproval)
                && OfflinePaymentStatus is not GameParticipantOfflinePaymentStatus.Paid)
            {
                OfflinePaymentStatus = game.PricePerPlayer > 0
                    ? GameParticipantOfflinePaymentStatus.Pending
                    : GameParticipantOfflinePaymentStatus.NotRequired;
            }
        }

        private void EnsureBelongsToGame(Game game)
        {
            if (GameId != game.Id)
            {
                throw new BusinessRuleException("Participant does not belong to this game.");
            }
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
                throw new ArgumentException(
                    "Approved date and time cannot be before joined date and time.",
                    nameof(approvedAt));
            }
        }

        private static void ValidateGameStartsAt(DateTimeOffset gameStartsAt)
        {
            if (gameStartsAt == default)
            {
                throw new ArgumentException(
                    "Game start date and time is required.",
                    nameof(gameStartsAt));
            }
        }

        private void ValidateParticipationEventTime(
            DateTimeOffset eventAt,
            string parameterName)
        {
            if (eventAt == default)
            {
                throw new ArgumentException(
                    "Participation event date and time is required.",
                    parameterName);
            }

            if (ApprovedAt is not null && eventAt < ApprovedAt)
            {
                throw new ArgumentException(
                    "Participation event date and time cannot be before approval date and time.",
                    parameterName);
            }
        }

        private static void ValidateAttendanceStatus(
            GameParticipantAttendanceStatus attendanceStatus)
        {
            if (attendanceStatus is GameParticipantAttendanceStatus.Unknown
                or GameParticipantAttendanceStatus.NotMarked
                || !Enum.IsDefined(attendanceStatus))
            {
                throw new ArgumentException(
                    "Attendance status is invalid.",
                    nameof(attendanceStatus));
            }
        }

        private static void ValidateOfflinePaymentStatus(
            GameParticipantOfflinePaymentStatus offlinePaymentStatus)
        {
            if (offlinePaymentStatus is GameParticipantOfflinePaymentStatus.Unknown
                || !Enum.IsDefined(offlinePaymentStatus))
            {
                throw new ArgumentException(
                    "Offline payment status is invalid.",
                    nameof(offlinePaymentStatus));
            }
        }
    }
}
