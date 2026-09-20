using VolleyHub.Domain.Common;

namespace VolleyHub.Domain.Games
{
    public sealed class GameRecurrence : AuditableEntity
    {
        public const int MinOccurrences = 2;
        public const int MaxOccurrences = 52;

        private GameRecurrence() { }

        public Guid Id { get; private set; }
        public Guid SourceGameId { get; private set; }
        public Guid OrganizerId { get; private set; }
        public Guid CourtId { get; private set; }
        public DateTimeOffset FirstStartsAt { get; private set; }
        public TimeSpan? Duration { get; private set; }
        public int OccurrenceCount { get; private set; }
        public int MaxPlayers { get; private set; }
        public decimal PricePerPlayer { get; private set; }
        public GameLevel RequiredLevel { get; private set; }
        public GameJoinPolicy JoinPolicy { get; private set; }
        public string? Description { get; private set; }
        public DateTimeOffset? CancelledAt { get; private set; }

        public static GameRecurrence Create(Guid id, Game source, DateTimeOffset firstStartsAt, int occurrenceCount, DateTimeOffset now)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Recurrence id is required.", nameof(id));
            }

            if (firstStartsAt <= now)
            {
                throw new ArgumentException("The first occurrence must start in the future.", nameof(firstStartsAt));
            }

            if (occurrenceCount is < MinOccurrences or > MaxOccurrences)
            {
                throw new ArgumentException($"Occurrence count must be between {MinOccurrences} and {MaxOccurrences}.", nameof(occurrenceCount));
            }

            var recurrence = new GameRecurrence
            {
                Id = id,
                SourceGameId = source.Id,
                OrganizerId = source.OrganizerId,
                FirstStartsAt = firstStartsAt.ToUniversalTime(),
                Duration = source.EndsAt - source.StartsAt,
                OccurrenceCount = occurrenceCount
            };

            // Validate the entire bounded schedule before any instances are persisted.
            var lastStartsAt = recurrence.GetStartsAt(occurrenceCount);
            if (recurrence.Duration is { } duration)
            {
                _ = lastStartsAt.Add(duration);
            }

            recurrence.CopySettings(source);
            return recurrence;
        }

        public DateTimeOffset GetStartsAt(int occurrenceNumber)
        {
            if (occurrenceNumber < 1 || occurrenceNumber > OccurrenceCount)
            {
                throw new ArgumentException("Occurrence number is outside this recurrence.", nameof(occurrenceNumber));
            }

            return FirstStartsAt.AddDays(7 * (occurrenceNumber - 1));
        }

        public Game CreateOccurrence(int occurrenceNumber)
        {
            EnsureActive();
            var startsAt = GetStartsAt(occurrenceNumber);
            var game = Game.Create(OrganizerId, CourtId, startsAt, Duration is { } duration ? startsAt.Add(duration) : null,
                MaxPlayers, PricePerPlayer, RequiredLevel, JoinPolicy, Description);
            game.AssignRecurrence(Id, occurrenceNumber);
            return game;
        }

        public IReadOnlyList<Game> SelectFutureOccurrences(IEnumerable<Game> games, int fromOccurrenceNumber, DateTimeOffset now)
        {
            EnsureActive();
            _ = GetStartsAt(fromOccurrenceNumber);
            return games.Where(game => game.RecurrenceId == Id && game.OccurrenceNumber >= fromOccurrenceNumber
                    && game.StartsAt > now && game.Status is GameStatus.Draft or GameStatus.Open or GameStatus.Full)
                .OrderBy(game => game.OccurrenceNumber).ToArray();
        }

        public void UpdateSettingsFrom(Game occurrence)
        {
            EnsureActive();
            if (occurrence.RecurrenceId != Id || occurrence.OrganizerId != OrganizerId)
            {
                throw new BusinessRuleException("Settings must come from an occurrence of this recurrence.");
            }

            CopySettings(occurrence);
        }

        public void Cancel(DateTimeOffset now)
        {
            EnsureActive();
            CancelledAt = now;
        }

        private void EnsureActive()
        {
            if (CancelledAt is not null)
            {
                throw new BusinessRuleException("The recurrence is cancelled.");
            }
        }

        private void CopySettings(Game game)
        {
            CourtId = game.CourtId;
            MaxPlayers = game.MaxPlayers;
            PricePerPlayer = game.PricePerPlayer;
            RequiredLevel = game.RequiredLevel;
            JoinPolicy = game.JoinPolicy;
            Description = game.Description;
        }
    }
}
