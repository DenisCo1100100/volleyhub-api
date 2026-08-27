using FluentAssertions;
using VolleyHub.Domain.Auth;
using VolleyHub.Domain.Common;

namespace VolleyHub.Domain.UnitTests.Auth
{
    public sealed class RefreshTokenTests
    {
        [Fact]
        public void Create_ShouldCreateActiveRefreshToken_WhenDataIsValid()
        {
            var userId = Guid.NewGuid();
            var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

            var refreshToken = RefreshToken.Create(
                userId,
                "token-hash",
                expiresAt);

            refreshToken.Id.Should().NotBeEmpty();
            refreshToken.UserId.Should().Be(userId);
            refreshToken.TokenHash.Should().Be("token-hash");
            refreshToken.ExpiresAt.Should().Be(expiresAt);
            refreshToken.RevokedAt.Should().BeNull();
            refreshToken.ReplacedByTokenId.Should().BeNull();
            refreshToken.IsRotated.Should().BeFalse();
        }

        [Fact]
        public void IsActive_ShouldReturnTrue_WhenTokenIsNotExpiredOrRevoked()
        {
            var now = DateTimeOffset.UtcNow;

            var refreshToken = RefreshToken.Create(
                Guid.NewGuid(),
                "token-hash",
                now.AddDays(7));

            refreshToken.IsActive(now).Should().BeTrue();
            refreshToken.IsExpired(now).Should().BeFalse();
        }

        [Fact]
        public void IsExpired_ShouldReturnTrue_WhenExpiryHasPassed()
        {
            var now = DateTimeOffset.UtcNow;

            var refreshToken = RefreshToken.Create(
                Guid.NewGuid(),
                "token-hash",
                now.AddMinutes(-1));

            refreshToken.IsExpired(now).Should().BeTrue();
            refreshToken.IsActive(now).Should().BeFalse();
        }

        [Fact]
        public void Revoke_ShouldRevokeRefreshToken()
        {
            var refreshToken = CreateRefreshToken();
            var revokedAt = DateTimeOffset.UtcNow;

            refreshToken.Revoke(revokedAt);

            refreshToken.RevokedAt.Should().Be(revokedAt);
            refreshToken.ReplacedByTokenId.Should().BeNull();
            refreshToken.IsActive(revokedAt).Should().BeFalse();
            refreshToken.IsRotated.Should().BeFalse();
        }

        [Fact]
        public void Rotate_ShouldRevokeTokenAndStoreReplacementId()
        {
            var refreshToken = CreateRefreshToken();
            var replacementTokenId = Guid.NewGuid();
            var revokedAt = DateTimeOffset.UtcNow;

            refreshToken.Rotate(
                replacementTokenId,
                revokedAt);

            refreshToken.RevokedAt.Should().Be(revokedAt);
            refreshToken.ReplacedByTokenId.Should().Be(replacementTokenId);
            refreshToken.IsRotated.Should().BeTrue();
            refreshToken.IsActive(revokedAt).Should().BeFalse();
        }

        [Fact]
        public void Revoke_ShouldThrowBusinessRuleException_WhenTokenIsAlreadyRevoked()
        {
            var refreshToken = CreateRefreshToken();
            refreshToken.Revoke(DateTimeOffset.UtcNow);

            Action act = () => refreshToken.Revoke(DateTimeOffset.UtcNow);

            act.Should().Throw<BusinessRuleException>();
        }

        [Fact]
        public void Rotate_ShouldThrowBusinessRuleException_WhenTokenIsAlreadyRevoked()
        {
            var refreshToken = CreateRefreshToken();
            refreshToken.Revoke(DateTimeOffset.UtcNow);

            Action act = () => refreshToken.Rotate(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow);

            act.Should().Throw<BusinessRuleException>();
        }

        [Fact]
        public void Rotate_ShouldThrowArgumentException_WhenReplacementTokenIdIsEmpty()
        {
            var refreshToken = CreateRefreshToken();

            Action act = () => refreshToken.Rotate(
                Guid.Empty,
                DateTimeOffset.UtcNow);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Rotate_ShouldThrowArgumentException_WhenReplacementTokenIsSameToken()
        {
            var refreshToken = CreateRefreshToken();

            Action act = () => refreshToken.Rotate(
                refreshToken.Id,
                DateTimeOffset.UtcNow);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenUserIdIsEmpty()
        {
            Action act = () => RefreshToken.Create(
                Guid.Empty,
                "token-hash",
                DateTimeOffset.UtcNow.AddDays(7));

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenTokenHashIsEmpty()
        {
            Action act = () => RefreshToken.Create(
                Guid.NewGuid(),
                string.Empty,
                DateTimeOffset.UtcNow.AddDays(7));

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenTokenHashIsTooLong()
        {
            var tokenHash = new string(
                'a',
                RefreshToken.MaxTokenHashLength + 1);

            Action act = () => RefreshToken.Create(
                Guid.NewGuid(),
                tokenHash,
                DateTimeOffset.UtcNow.AddDays(7));

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenExpiresAtIsDefault()
        {
            Action act = () => RefreshToken.Create(
                Guid.NewGuid(),
                "token-hash",
                default);

            act.Should().Throw<ArgumentException>();
        }

        private static RefreshToken CreateRefreshToken()
        {
            return RefreshToken.Create(
                Guid.NewGuid(),
                "token-hash",
                DateTimeOffset.UtcNow.AddDays(7));
        }
    }
}