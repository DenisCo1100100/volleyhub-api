using VolleyHub.Domain.Common;

namespace VolleyHub.Domain.Games
{
    public sealed class GameTemplate : AuditableEntity
    {
        public const int MaxNameLength = 100;

        private GameTemplate() { }

        public Guid Id { get; private set; }
        public Guid OrganizerId { get; private set; }
        public string Name { get; private set; } = null!;
        public Guid CourtId { get; private set; }
        public TimeSpan? Duration { get; private set; }
        public int MaxPlayers { get; private set; }
        public decimal PricePerPlayer { get; private set; }
        public GameLevel RequiredLevel { get; private set; }
        public GameJoinPolicy JoinPolicy { get; private set; }
        public string? Description { get; private set; }

        public static GameTemplate Create(Guid organizerId, string name, Guid courtId, TimeSpan? duration, int maxPlayers,
            decimal pricePerPlayer, GameLevel requiredLevel, GameJoinPolicy joinPolicy, string? description)
        {
            if (organizerId == Guid.Empty)
            {
                throw new ArgumentException("Organizer id is required.", nameof(organizerId));
            }

            var template = new GameTemplate { Id = Guid.NewGuid(), OrganizerId = organizerId };
            template.Update(name, courtId, duration, maxPlayers, pricePerPlayer, requiredLevel, joinPolicy, description);
            return template;
        }

        public void Update(string name, Guid courtId, TimeSpan? duration, int maxPlayers, decimal pricePerPlayer,
            GameLevel requiredLevel, GameJoinPolicy joinPolicy, string? description)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MaxNameLength)
            {
                throw new ArgumentException($"Template name is required and must be {MaxNameLength} characters or less.", nameof(name));
            }

            if (duration is { } value && value <= TimeSpan.Zero)
            {
                throw new ArgumentException("Template duration must be positive.", nameof(duration));
            }

            Game.ValidateSettings(courtId, maxPlayers, pricePerPlayer, requiredLevel, joinPolicy, description);

            Name = name.Trim();
            CourtId = courtId;
            Duration = duration;
            MaxPlayers = maxPlayers;
            PricePerPlayer = pricePerPlayer;
            RequiredLevel = requiredLevel;
            JoinPolicy = joinPolicy;
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        }

        public Game CreateGame(DateTimeOffset startsAt)
        {
            startsAt = startsAt.ToUniversalTime();
            return Game.Create(OrganizerId, CourtId, startsAt, Duration is { } duration ? startsAt.Add(duration) : null,
                MaxPlayers, PricePerPlayer, RequiredLevel, JoinPolicy, Description);
        }
    }
}
