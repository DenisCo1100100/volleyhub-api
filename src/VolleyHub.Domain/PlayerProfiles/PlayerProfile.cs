using VolleyHub.Domain.Common;

namespace VolleyHub.Domain.PlayerProfiles
{
    public sealed class PlayerProfile : AuditableEntity
    {
        public const int MaxDisplayNameLength = 100;
        public const int MaxCityLength = 100;
        public const int MaxBioLength = 1000;

        private PlayerProfile() { }

        private PlayerProfile(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public string DisplayName { get; private set; } = string.Empty;
        public PlayerSkillLevel SkillLevel { get; private set; }
        public string? City { get; private set; }
        public string? Bio { get; private set; }
        public bool IsDeleted { get; private set; }

        public static PlayerProfile Create(
            Guid userId,
            string displayName,
            PlayerSkillLevel skillLevel,
            string? city,
            string? bio)
        {
            var playerProfile = new PlayerProfile(Guid.NewGuid());

            playerProfile.ApplyDetails(
                userId,
                displayName,
                skillLevel,
                city,
                bio);

            return playerProfile;
        }

        public void UpdateDetails(
            string displayName,
            PlayerSkillLevel skillLevel,
            string? city,
            string? bio)
        {
            ValidateDisplayName(displayName);
            ValidateSkillLevel(skillLevel);
            ValidateCity(city);
            ValidateBio(bio);

            DisplayName = NormalizeRequiredText(displayName);
            SkillLevel = skillLevel;
            City = NormalizeOptionalText(city);
            Bio = NormalizeOptionalText(bio);
        }

        public void Delete()
        {
            IsDeleted = true;
        }

        private void ApplyDetails(
            Guid userId,
            string displayName,
            PlayerSkillLevel skillLevel,
            string? city,
            string? bio)
        {
            ValidateUserId(userId);
            ValidateDisplayName(displayName);
            ValidateSkillLevel(skillLevel);
            ValidateCity(city);
            ValidateBio(bio);

            UserId = userId;
            DisplayName = NormalizeRequiredText(displayName);
            SkillLevel = skillLevel;
            City = NormalizeOptionalText(city);
            Bio = NormalizeOptionalText(bio);
        }

        private static void ValidateUserId(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User id is required.", nameof(userId));
            }
        }

        private static void ValidateDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name is required.", nameof(displayName));
            }

            if (displayName.Trim().Length > MaxDisplayNameLength)
            {
                throw new ArgumentException(
                    $"Display name must be {MaxDisplayNameLength} characters or less.",
                    nameof(displayName));
            }
        }

        private static void ValidateSkillLevel(PlayerSkillLevel skillLevel)
        {
            if (skillLevel is PlayerSkillLevel.Unknown || !Enum.IsDefined(skillLevel))
            {
                throw new ArgumentException("Player skill level is invalid.", nameof(skillLevel));
            }
        }

        private static void ValidateCity(string? city)
        {
            if (city is not null && city.Trim().Length > MaxCityLength)
            {
                throw new ArgumentException(
                    $"City must be {MaxCityLength} characters or less.",
                    nameof(city));
            }
        }

        private static void ValidateBio(string? bio)
        {
            if (bio is not null && bio.Trim().Length > MaxBioLength)
            {
                throw new ArgumentException(
                    $"Bio must be {MaxBioLength} characters or less.",
                    nameof(bio));
            }
        }

        private static string NormalizeRequiredText(string value)
        {
            return value.Trim();
        }

        private static string? NormalizeOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}