using VolleyHub.Domain.Common;

namespace VolleyHub.Domain.Auth
{
    public sealed class RefreshToken : AuditableEntity
    {
        public const int MaxTokenHashLength = 128;

        private RefreshToken() { }

        private RefreshToken(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public string TokenHash { get; private set; } = null!;
        public DateTimeOffset ExpiresAt { get; private set; }
        public DateTimeOffset? RevokedAt { get; private set; }
        public Guid? ReplacedByTokenId { get; private set; }

        public bool IsRotated => ReplacedByTokenId is not null;

        public static RefreshToken Create(Guid userId, string tokenHash, DateTimeOffset expiresAt)
        {
            ValidateUserId(userId);
            ValidateTokenHash(tokenHash);
            ValidateExpiresAt(expiresAt);

            return new RefreshToken(Guid.NewGuid())
            {
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt
            };
        }

        public bool IsExpired(DateTimeOffset now)
        {
            return now >= ExpiresAt;
        }

        public bool IsActive(DateTimeOffset now)
        {
            return RevokedAt is null && !IsExpired(now);
        }

        public void Revoke(DateTimeOffset revokedAt)
        {
            EnsureNotRevoked();
            ValidateRevokedAt(revokedAt);

            RevokedAt = revokedAt;
        }

        public void Rotate(Guid replacementTokenId, DateTimeOffset revokedAt)
        {
            EnsureNotRevoked();

            if (replacementTokenId == Guid.Empty)
            {
                throw new ArgumentException("Replacement token id is required.", nameof(replacementTokenId));
            }

            if (replacementTokenId == Id)
            {
                throw new ArgumentException("Refresh token cannot replace itself.", nameof(replacementTokenId));
            }

            ValidateRevokedAt(revokedAt);

            RevokedAt = revokedAt;
            ReplacedByTokenId = replacementTokenId;
        }

        private void EnsureNotRevoked()
        {
            if (RevokedAt is not null)
            {
                throw new BusinessRuleException("Refresh token is already revoked.");
            }
        }

        private static void ValidateUserId(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User id is required.", nameof(userId));
            }
        }

        private static void ValidateTokenHash(string tokenHash)
        {
            if (string.IsNullOrWhiteSpace(tokenHash))
            {
                throw new ArgumentException("Token hash is required.", nameof(tokenHash));
            }

            if (tokenHash.Length > MaxTokenHashLength)
            {
                throw new ArgumentException($"Token hash must be {MaxTokenHashLength} characters or less.", nameof(tokenHash));
            }
        }

        private static void ValidateExpiresAt(DateTimeOffset expiresAt)
        {
            if (expiresAt == default)
            {
                throw new ArgumentException("Expiry date and time is required.", nameof(expiresAt));
            }
        }

        private static void ValidateRevokedAt(DateTimeOffset revokedAt)
        {
            if (revokedAt == default)
            {
                throw new ArgumentException("Revocation date and time is required.", nameof(revokedAt));
            }
        }
    }
}