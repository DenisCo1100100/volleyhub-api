using System;
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
        public Guid CourtId { get; private set; }
        public DateTimeOffset StartsAt { get; private set; }
        public int MaxPlayers { get; private set; }
        public string? Description { get; private set; }
        public GameStatus Status { get; private set; }

        public static Game Create(
            Guid courtId,
            DateTimeOffset startsAt,
            int maxPlayers,
            string? description)
        {
            var game = new Game(Guid.NewGuid())
            {
                Status = GameStatus.Scheduled
            };

            game.ApplyDetails(
                courtId,
                startsAt,
                maxPlayers,
                description);

            return game;
        }

        public void UpdateDetails(
            Guid courtId,
            DateTimeOffset startsAt,
            int maxPlayers,
            string? description)
        {
            EnsureScheduled();

            ApplyDetails(
                courtId,
                startsAt,
                maxPlayers,
                description);
        }

        public void Cancel()
        {
            EnsureScheduled();

            Status = GameStatus.Cancelled;
        }

        public void Complete()
        {
            EnsureScheduled();

            Status = GameStatus.Completed;
        }

        private void ApplyDetails(
            Guid courtId,
            DateTimeOffset startsAt,
            int maxPlayers,
            string? description)
        {
            ValidateCourtId(courtId);
            ValidateStartsAt(startsAt);
            ValidateMaxPlayers(maxPlayers);
            ValidateDescription(description);

            CourtId = courtId;
            StartsAt = startsAt;
            MaxPlayers = maxPlayers;
            Description = NormalizeOptionalText(description);
        }

        private void EnsureScheduled()
        {
            if (Status is not GameStatus.Scheduled)
            {
                throw new InvalidOperationException("Only scheduled games can be changed.");
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

        private static void ValidateMaxPlayers(int maxPlayers)
        {
            if (maxPlayers is < MinPlayers or > MaxPlayersLimit)
            {
                throw new ArgumentException(
                    $"Max players must be between {MinPlayers} and {MaxPlayersLimit}.",
                    nameof(maxPlayers));
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