using VolleyHub.Domain.Common;

namespace VolleyHub.Domain.Games
{
    public sealed class Game : AuditableEntity
    {
        public const int MinPlayers = 2;
        public const int MaxPlayersLimit = 24;
        public const int MaxDescriptionLength = 2000;

        private Game() { }

        private Game(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; private set; }
        public Guid OrganizerId { get; private set; }
        public Guid CourtId { get; private set; }
        public Guid? RecurrenceId { get; private set; }
        public int? OccurrenceNumber { get; private set; }
        public DateTimeOffset StartsAt { get; private set; }
        public DateTimeOffset? EndsAt { get; private set; }
        public int MaxPlayers { get; private set; }
        public decimal PricePerPlayer { get; private set; }
        public GameLevel RequiredLevel { get; private set; }
        public GameJoinPolicy JoinPolicy { get; private set; }
        public string? Description { get; private set; }
        public GameStatus Status { get; private set; }

        public static Game Create(
            Guid organizerId,
            Guid courtId,
            DateTimeOffset startsAt,
            DateTimeOffset? endsAt,
            int maxPlayers,
            decimal pricePerPlayer,
            GameLevel requiredLevel,
            GameJoinPolicy joinPolicy,
            string? description)
        {
            var game = new Game(Guid.NewGuid())
            {
                Status = GameStatus.Open
            };

            game.ApplyDetails(
                organizerId,
                courtId,
                startsAt,
                endsAt,
                maxPlayers,
                pricePerPlayer,
                requiredLevel,
                joinPolicy,
                description);

            return game;
        }

        internal void AssignRecurrence(Guid recurrenceId, int occurrenceNumber)
        {
            RecurrenceId = recurrenceId;
            OccurrenceNumber = occurrenceNumber;
        }

        public void UpdateDetails(
            Guid organizerId,
            Guid courtId,
            DateTimeOffset startsAt,
            DateTimeOffset? endsAt,
            int maxPlayers,
            decimal pricePerPlayer,
            GameLevel requiredLevel,
            GameJoinPolicy joinPolicy,
            string? description)
        {
            EnsureCanBeChanged();

            ApplyDetails(
                organizerId,
                courtId,
                startsAt,
                endsAt,
                maxPlayers,
                pricePerPlayer,
                requiredLevel,
                joinPolicy,
                description);
        }

        public void EnsureCanJoinWaitlist(int approvedParticipantCount, DateTimeOffset now)
        {
            if (Status is not GameStatus.Full || approvedParticipantCount < MaxPlayers)
            {
                throw new BusinessRuleException("Only full games accept waitlisted players.");
            }

            if (JoinPolicy is not GameJoinPolicy.Open and not GameJoinPolicy.ApprovalRequired)
            {
                throw new BusinessRuleException("Invite-only games do not accept waitlisted players.");
            }

            EnsureWaitlistBeforeStart(now);
        }

        public void EnsureCanPromoteFromWaitlist(int approvedParticipantCount, DateTimeOffset now)
        {
            if (Status is not GameStatus.Open || approvedParticipantCount >= MaxPlayers)
            {
                throw new BusinessRuleException("Waitlist promotion requires an open game with available capacity.");
            }

            EnsureWaitlistBeforeStart(now);
        }

        public void EnsureCapacity(int approvedParticipantCount, int maxPlayers)
        {
            if (maxPlayers < approvedParticipantCount)
            {
                throw new BusinessRuleException("Maximum players cannot be less than the approved participant count.");
            }
        }

        public void EnsureCanChangePrice(decimal pricePerPlayer, IEnumerable<GameParticipant> participants)
        {
            if (pricePerPlayer != PricePerPlayer && participants.Any(participant =>
                participant.GameId == Id && participant.OfflinePaymentStatus is GameParticipantOfflinePaymentStatus.Paid))
            {
                throw new BusinessRuleException("Price cannot be changed while participants have recorded payments.");
            }
        }

        private void EnsureWaitlistBeforeStart(DateTimeOffset now)
        {
            if (now >= StartsAt)
            {
                throw new BusinessRuleException("Players cannot join or be promoted from the waitlist after the game has started.");
            }
        }

        public void MarkAsFull()
        {
            if (Status is not GameStatus.Open)
            {
                throw new BusinessRuleException("Only open games can be marked as full.");
            }

            Status = GameStatus.Full;
        }

        public void Reopen()
        {
            if (Status is not GameStatus.Full)
            {
                throw new BusinessRuleException("Only full games can be reopened.");
            }

            Status = GameStatus.Open;
        }

        public void Cancel()
        {
            if (Status is GameStatus.Cancelled)
            {
                throw new BusinessRuleException("Game is already cancelled.");
            }

            if (Status is GameStatus.Completed)
            {
                throw new BusinessRuleException("Completed games cannot be cancelled.");
            }

            Status = GameStatus.Cancelled;
        }

        public void Complete()
        {
            if (Status is not GameStatus.Open and not GameStatus.Full)
            {
                throw new BusinessRuleException("Only open or full games can be completed.");
            }

            Status = GameStatus.Completed;
        }

        private void ApplyDetails(
            Guid organizerId,
            Guid courtId,
            DateTimeOffset startsAt,
            DateTimeOffset? endsAt,
            int maxPlayers,
            decimal pricePerPlayer,
            GameLevel requiredLevel,
            GameJoinPolicy joinPolicy,
            string? description)
        {
            ValidateOrganizerId(organizerId);
            ValidateCourtId(courtId);
            ValidateStartsAt(startsAt);
            ValidateEndsAt(startsAt, endsAt);
            ValidateMaxPlayers(maxPlayers);
            ValidatePricePerPlayer(pricePerPlayer);
            ValidateRequiredLevel(requiredLevel);
            ValidateJoinPolicy(joinPolicy);
            ValidateDescription(description);

            OrganizerId = organizerId;
            CourtId = courtId;
            StartsAt = startsAt;
            EndsAt = endsAt;
            MaxPlayers = maxPlayers;
            PricePerPlayer = pricePerPlayer;
            RequiredLevel = requiredLevel;
            JoinPolicy = joinPolicy;
            Description = NormalizeOptionalText(description);
        }

        private void EnsureCanBeChanged()
        {
            if (Status is not GameStatus.Draft and not GameStatus.Open)
            {
                throw new BusinessRuleException("Only draft or open games can be changed.");
            }
        }

        private static void ValidateOrganizerId(Guid organizerId)
        {
            if (organizerId == Guid.Empty)
            {
                throw new ArgumentException("Organizer id is required.", nameof(organizerId));
            }
        }

        private static void ValidateCourtId(Guid courtId)
        {
            if (courtId == Guid.Empty)
            {
                throw new ArgumentException("Court id is required.", nameof(courtId));
            }
        }

        private static void ValidateStartsAt(DateTimeOffset startsAt)
        {
            if (startsAt == default)
            {
                throw new ArgumentException("Game start date and time is required.", nameof(startsAt));
            }
        }

        private static void ValidateEndsAt(
            DateTimeOffset startsAt,
            DateTimeOffset? endsAt)
        {
            if (endsAt is not null && endsAt <= startsAt)
            {
                throw new ArgumentException("Game end date and time must be after start date and time.", nameof(endsAt));
            }
        }

        private static void ValidateMaxPlayers(int maxPlayers)
        {
            if (maxPlayers is < MinPlayers or > MaxPlayersLimit)
            {
                throw new ArgumentException(
                    $"Max players must be between {MinPlayers} and {MaxPlayersLimit}.",
                    nameof(maxPlayers));
            }
        }

        private static void ValidatePricePerPlayer(decimal pricePerPlayer)
        {
            if (pricePerPlayer < 0)
            {
                throw new ArgumentException("Price per player cannot be negative.", nameof(pricePerPlayer));
            }
        }

        private static void ValidateRequiredLevel(GameLevel requiredLevel)
        {
            if (requiredLevel is GameLevel.Unknown || !Enum.IsDefined(requiredLevel))
            {
                throw new ArgumentException("Game level is invalid.", nameof(requiredLevel));
            }
        }

        private static void ValidateJoinPolicy(GameJoinPolicy joinPolicy)
        {
            if (joinPolicy is GameJoinPolicy.Unknown || !Enum.IsDefined(joinPolicy))
            {
                throw new ArgumentException("Game join policy is invalid.", nameof(joinPolicy));
            }
        }

        private static void ValidateDescription(string? description)
        {
            if (description is not null && description.Trim().Length > MaxDescriptionLength)
            {
                throw new ArgumentException(
                    $"Game description must be {MaxDescriptionLength} characters or less.",
                    nameof(description));
            }
        }

        private static string? NormalizeOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
