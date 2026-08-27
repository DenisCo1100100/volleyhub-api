using FluentAssertions;
using Moq;
using VolleyHub.Application.Auth.Commands.Logout;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Auth;

namespace VolleyHub.Application.UnitTests.Auth.Commands.Logout
{
    public sealed class LogoutCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldRevokeRefreshToken_WhenTokenIsActive()
        {
            var now = DateTimeOffset.UtcNow;
            var refreshToken = RefreshToken.Create(Guid.NewGuid(), "token-hash", now.AddDays(10));

            var refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
            var refreshTokenGeneratorMock = new Mock<IRefreshTokenGenerator>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            refreshTokenGeneratorMock
                .Setup(generator => generator.HashToken("refresh-token"))
                .Returns("token-hash");

            refreshTokenRepositoryMock
                .Setup(repository => repository.GetByTokenHashAsync("token-hash", It.IsAny<CancellationToken>()))
                .ReturnsAsync(refreshToken);

            dateTimeProviderMock
                .Setup(provider => provider.UtcNow)
                .Returns(now);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new LogoutCommandHandler(
                refreshTokenRepositoryMock.Object,
                refreshTokenGeneratorMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            await handler.Handle(
                new LogoutCommand("refresh-token"),
                CancellationToken.None);

            refreshToken.RevokedAt.Should().Be(now);

            refreshTokenRepositoryMock.Verify(repository => repository.Update(refreshToken), Times.Once);
            unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldDoNothing_WhenRefreshTokenIsMissing()
        {
            var refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
            var refreshTokenGeneratorMock = new Mock<IRefreshTokenGenerator>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            var handler = new LogoutCommandHandler(
                refreshTokenRepositoryMock.Object,
                refreshTokenGeneratorMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            await handler.Handle(
                new LogoutCommand(null),
                CancellationToken.None);

            refreshTokenGeneratorMock.Verify(generator => generator.HashToken(It.IsAny<string>()), Times.Never);
            unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldDoNothing_WhenRefreshTokenDoesNotExist()
        {
            var refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
            var refreshTokenGeneratorMock = new Mock<IRefreshTokenGenerator>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            refreshTokenGeneratorMock
                .Setup(generator => generator.HashToken("invalid-token"))
                .Returns("invalid-token-hash");

            refreshTokenRepositoryMock
                .Setup(repository => repository.GetByTokenHashAsync("invalid-token-hash", It.IsAny<CancellationToken>()))
                .ReturnsAsync((RefreshToken?)null);

            var handler = new LogoutCommandHandler(
                refreshTokenRepositoryMock.Object,
                refreshTokenGeneratorMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            await handler.Handle(
                new LogoutCommand("invalid-token"),
                CancellationToken.None);

            refreshTokenRepositoryMock.Verify(repository => repository.Update(It.IsAny<RefreshToken>()), Times.Never);
            unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}