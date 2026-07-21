using VolleyHub.Domain.Common;

namespace VolleyHub.Domain.Courts
{
    public sealed class Court : AuditableEntity
    {
        public const int MaxNameLength = 150;
        public const int MaxAddressLength = 500;
        public const int MaxDescriptionLength = 2000;

        private Court()
        {
        }

        private Court(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; private set; }

        public Guid? OwnerPlayerProfileId { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string Address { get; private set; } = string.Empty;

        public double Latitude { get; private set; }

        public double Longitude { get; private set; }

        public CourtSurfaceType SurfaceType { get; private set; }

        public bool IsIndoor { get; private set; }

        public string? Description { get; private set; }

        public bool IsDeleted { get; private set; }

        public static Court Create(
            Guid ownerPlayerProfileId,
            string name,
            string address,
            double latitude,
            double longitude,
            CourtSurfaceType surfaceType,
            bool isIndoor,
            string? description)
        {
            ValidateOwnerPlayerProfileId(ownerPlayerProfileId);

            var court = new Court(Guid.NewGuid())
            {
                OwnerPlayerProfileId = ownerPlayerProfileId
            };

            court.ApplyDetails(
                name,
                address,
                latitude,
                longitude,
                surfaceType,
                isIndoor,
                description);

            return court;
        }

        public bool IsOwnedBy(Guid playerProfileId)
        {
            return playerProfileId != Guid.Empty
                && OwnerPlayerProfileId == playerProfileId;
        }

        public void Update(
            string name,
            string address,
            double latitude,
            double longitude,
            CourtSurfaceType surfaceType,
            bool isIndoor,
            string? description)
        {
            ApplyDetails(
                name,
                address,
                latitude,
                longitude,
                surfaceType,
                isIndoor,
                description);
        }

        public void Delete()
        {
            IsDeleted = true;
        }

        private void ApplyDetails(
            string name,
            string address,
            double latitude,
            double longitude,
            CourtSurfaceType surfaceType,
            bool isIndoor,
            string? description)
        {
            ValidateName(name);
            ValidateAddress(address);
            ValidateCoordinates(latitude, longitude);
            ValidateDescription(description);

            Name = NormalizeRequiredText(name);
            Address = NormalizeRequiredText(address);
            Latitude = latitude;
            Longitude = longitude;
            SurfaceType = surfaceType;
            IsIndoor = isIndoor;
            Description = NormalizeOptionalText(description);
        }

        private static void ValidateOwnerPlayerProfileId(
            Guid ownerPlayerProfileId)
        {
            if (ownerPlayerProfileId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Court owner player profile id is required.",
                    nameof(ownerPlayerProfileId));
            }
        }

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(
                    nameof(name),
                    "Court name is required.");
            }

            if (name.Length > MaxNameLength)
            {
                throw new ArgumentException(
                    $"Court name must be {MaxNameLength} characters or less.",
                    nameof(name));
            }
        }

        private static void ValidateAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentNullException(
                    nameof(address),
                    "Court address is required.");
            }

            if (address.Length > MaxAddressLength)
            {
                throw new ArgumentException(
                    $"Court address must be {MaxAddressLength} characters or less.",
                    nameof(address));
            }
        }

        private static void ValidateCoordinates(
            double latitude,
            double longitude)
        {
            if (latitude is < -90 or > 90)
            {
                throw new ArgumentException(
                    "Latitude must be between -90 and 90.",
                    nameof(latitude));
            }

            if (longitude is < -180 or > 180)
            {
                throw new ArgumentException(
                    "Longitude must be between -180 and 180.",
                    nameof(longitude));
            }
        }

        private static void ValidateDescription(
            string? description)
        {
            if (description is not null
                && description.Trim().Length > MaxDescriptionLength)
            {
                throw new ArgumentException(
                    $"Court description must be {MaxDescriptionLength} characters or less.",
                    nameof(description));
            }
        }

        private static string NormalizeRequiredText(
            string value)
        {
            return value.Trim();
        }

        private static string? NormalizeOptionalText(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}