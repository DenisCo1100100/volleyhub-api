using FluentAssertions;
using Moq;
using VolleyHub.Application.Auth.Commands.RefreshSession;
using VolleyHub.Application.Auth.Common;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Auth;
using VolleyHub.Domain.Users;

namespace VolleyHub.Application.UnitTests.Auth.Commands.RefreshSession
{
    public sealed class RefreshSessionCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldRotateRefreshTokenAndReturnNewSession_WhenTokenIsValid()
        {
            var now = DateTimeOffset.UtcNow;
            var user = User.Create("test@example.com", "hashed-password");
            var currentRefreshToken = RefreshToken.Create(user.Id, "current-token-hash", now.AddDays(10));
            var replacementExpiresAt = now.AddDays(30);

            var refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
            var userRepositoryMock = new Mock<IUserRepository>();
            var refreshTokenGeneratorMock = new Mock<IRefreshTokenGenerator>();
            var jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            RefreshToken? addedRefreshToken = null;

            refreshTokenGeneratorMock
                .Setup(generator => generator.HashToken("current-token"))
                .Returns("current-token-hash");

            refreshTokenRepositoryMock
                .Setup(repository => repository.GetByTokenHashAsync("current-token-hash", It.IsAny<CancellationToken>()))
                .ReturnsAsync(currentRefreshToken);

            userRepositoryMock
                .Setup(repository => repository.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            refreshTokenGeneratorMock
                .Setup(generator => generator.Generate())
                .Returns(new GeneratedRefreshToken(
                    "replacement-token",
                    "replacement-token-hash",
                    replacementExpiresAt));

            refreshTokenRepositoryMock
                .Setup(repository => repository.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
                .Callback<RefreshToken, CancellationToken>((refreshToken, _) => addedRefreshToken = refreshToken)
                .Returns(Task.CompletedTask);

            jwtTokenGeneratorMock
                .Setup(generator => generator.GenerateToken(user))
                .Returns("new-access-token");

            dateTimeProviderMock
                .Setup(provider => provider.UtcNow)
                .Returns(now);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new RefreshSessionCommandHandler(
                refreshTokenRepositoryMock.Object,
                userRepositoryMock.Object,
                refreshTokenGeneratorMock.Object,
                jwtTokenGeneratorMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var result = await handler.Handle(
                new RefreshSessionCommand("current-token"),
                CancellationToken.None);

            addedRefreshToken.Should().NotBeNull();
            addedRefreshToken!.UserId.Should().Be(user.Id);
            addedRefreshToken.TokenHash.Should().Be("replacement-token-hash");
            addedRefreshToken.ExpiresAt.Should().Be(replacementExpiresAt);

            currentRefreshToken.RevokedAt.Should().Be(now);
            currentRefreshToken.ReplacedByTokenId.Should().Be(addedRefreshToken.Id);
            currentRefreshToken.IsRotated.Should().BeTrue();

            result.UserId.Should().Be(user.Id);
            result.Email.Should().Be(user.Email);
            result.AccessToken.Should().Be("new-access-token");
            result.RefreshToken.Should().Be("replacement-token");
            result.RefreshTokenExpiresAt.Should().Be(replacementExpiresAt);

            refreshTokenRepositoryMock.Verify(repository => repository.Update(currentRefreshToken), Times.Once);
            unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldRevokeReplacementTokenAndRejectRequest_WhenRotatedTokenIsReused()
        {
            var now = DateTimeOffset.UtcNow;

            var oldRefreshToken = RefreshToken.Create(
                Guid.NewGuid(),
                "old-token-hash",
                now.AddDays(10));

            var replacementRefreshToken = RefreshToken.Create(
                oldRefreshToken.UserId,
                "replacement-token-hash",
                now.AddDays(20));

            oldRefreshToken.Rotate(
                replacementRefreshToken.Id,
                now.AddMinutes(-10));

            var refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
            var userRepositoryMock = new Mock<IUserRepository>();
            var refreshTokenGeneratorMock = new Mock<IRefreshTokenGenerator>();
            var jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            refreshTokenGeneratorMock
                .Setup(generator => generator.HashToken("old-token"))
                .Returns("old-token-hash");

            refreshTokenRepositoryMock
                .Setup(repository => repository.GetByTokenHashAsync("old-token-hash", It.IsAny<CancellationToken>()))
                .ReturnsAsync(oldRefreshToken);

            refreshTokenRepositoryMock
                .Setup(repository => repository.GetByIdAsync(replacementRefreshToken.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(replacementRefreshToken);

            dateTimeProviderMock
                .Setup(provider => provider.UtcNow)
                .Returns(now);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new RefreshSessionCommandHandler(
                refreshTokenRepositoryMock.Object,
                userRepositoryMock.Object,
                refreshTokenGeneratorMock.Object,
                jwtTokenGeneratorMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () => await handler.Handle(
                new RefreshSessionCommand("old-token"),
                CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedException>();

            replacementRefreshToken.RevokedAt.Should().Be(now);

            refreshTokenRepositoryMock.Verify(repository => repository.Update(replacementRefreshToken), Times.Once);
            unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            refreshTokenGeneratorMock.Verify(generator => generator.Generate(), Times.Never);
            jwtTokenGeneratorMock.Verify(generator => generator.GenerateToken(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldRejectRequest_WhenRefreshTokenIsExpired()
        {
            var now = DateTimeOffset.UtcNow;
            var refreshToken = RefreshToken.Create(Guid.NewGuid(), "expired-token-hash", now.AddMinutes(-1));

            var refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
            var userRepositoryMock = new Mock<IUserRepository>();
            var refreshTokenGeneratorMock = new Mock<IRefreshTokenGenerator>();
            var jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            refreshTokenGeneratorMock
                .Setup(generator => generator.HashToken("expired-token"))
                .Returns("expired-token-hash");

            refreshTokenRepositoryMock
                .Setup(repository => repository.GetByTokenHashAsync("expired-token-hash", It.IsAny<CancellationToken>()))
                .ReturnsAsync(refreshToken);

            dateTimeProviderMock
                .Setup(provider => provider.UtcNow)
                .Returns(now);

            var handler = new RefreshSessionCommandHandler(
                refreshTokenRepositoryMock.Object,
                userRepositoryMock.Object,
                refreshTokenGeneratorMock.Object,
                jwtTokenGeneratorMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () => await handler.Handle(
                new RefreshSessionCommand("expired-token"),
                CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedException>();

            refreshTokenGeneratorMock.Verify(generator => generator.Generate(), Times.Never);
            unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldRejectRequest_WhenRefreshTokenDoesNotExist()
        {
            var refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
            var userRepositoryMock = new Mock<IUserRepository>();
            var refreshTokenGeneratorMock = new Mock<IRefreshTokenGenerator>();
            var jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            refreshTokenGeneratorMock
                .Setup(generator => generator.HashToken("invalid-token"))
                .Returns("invalid-token-hash");

            refreshTokenRepositoryMock
                .Setup(repository => repository.GetByTokenHashAsync("invalid-token-hash", It.IsAny<CancellationToken>()))
                .ReturnsAsync((RefreshToken?)null);

            var handler = new RefreshSessionCommandHandler(
                refreshTokenRepositoryMock.Object,
                userRepositoryMock.Object,
                refreshTokenGeneratorMock.Object,
                jwtTokenGeneratorMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () => await handler.Handle(
                new RefreshSessionCommand("invalid-token"),
                CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedException>();

            refreshTokenGeneratorMock.Verify(generator => generator.Generate(), Times.Never);
            unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}