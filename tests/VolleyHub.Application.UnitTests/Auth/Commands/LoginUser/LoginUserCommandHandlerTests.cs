using FluentAssertions;
using Moq;
using VolleyHub.Application.Auth.Commands.LoginUser;
using VolleyHub.Application.Auth.Common;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Auth;
using VolleyHub.Domain.Users;

namespace VolleyHub.Application.UnitTests.Auth.Commands.LoginUser
{
    public sealed class LoginUserCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldCreateRefreshSessionAndReturnAuthResult_WhenCredentialsAreValid()
        {
            var user = User.Create(
                "test@example.com",
                "hashed-password");

            var refreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(30);

            var userRepositoryMock = new Mock<IUserRepository>();
            var refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
            var passwordHasherMock = new Mock<IPasswordHasher>();
            var jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
            var refreshTokenGeneratorMock = new Mock<IRefreshTokenGenerator>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();

            RefreshToken? addedRefreshToken = null;

            userRepositoryMock
                .Setup(repository => repository.GetByEmailAsync(
                    "test@example.com",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            passwordHasherMock
                .Setup(hasher => hasher.Verify(
                    "password123",
                    user.PasswordHash))
                .Returns(true);

            jwtTokenGeneratorMock
                .Setup(generator => generator.GenerateToken(user))
                .Returns("access-token");

            refreshTokenGeneratorMock
                .Setup(generator => generator.Generate())
                .Returns(new GeneratedRefreshToken(
                    "refresh-token",
                    "refresh-token-hash",
                    refreshTokenExpiresAt));

            refreshTokenRepositoryMock
                .Setup(repository => repository.AddAsync(
                    It.IsAny<RefreshToken>(),
                    It.IsAny<CancellationToken>()))
                .Callback<RefreshToken, CancellationToken>((refreshToken, _) => addedRefreshToken = refreshToken)
                .Returns(Task.CompletedTask);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new LoginUserCommandHandler(
                userRepositoryMock.Object,
                refreshTokenRepositoryMock.Object,
                passwordHasherMock.Object,
                jwtTokenGeneratorMock.Object,
                refreshTokenGeneratorMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var result = await handler.Handle(
                new LoginUserCommand(
                    Email: "Test@Example.com",
                    Password: "password123"),
                CancellationToken.None);

            result.UserId.Should().Be(user.Id);
            result.Email.Should().Be(user.Email);
            result.AccessToken.Should().Be("access-token");
            result.RefreshToken.Should().Be("refresh-token");
            result.RefreshTokenExpiresAt.Should().Be(refreshTokenExpiresAt);

            addedRefreshToken.Should().NotBeNull();
            addedRefreshToken!.UserId.Should().Be(user.Id);
            addedRefreshToken.TokenHash.Should().Be("refresh-token-hash");
            addedRefreshToken.ExpiresAt.Should().Be(refreshTokenExpiresAt);

            refreshTokenRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<RefreshToken>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowArgumentException_WhenUserDoesNotExist()
        {
            var userRepositoryMock = new Mock<IUserRepository>();
            var refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
            var passwordHasherMock = new Mock<IPasswordHasher>();
            var jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
            var refreshTokenGeneratorMock = new Mock<IRefreshTokenGenerator>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();

            userRepositoryMock
                .Setup(repository => repository.GetByEmailAsync(
                    "test@example.com",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            var handler = new LoginUserCommandHandler(
                userRepositoryMock.Object,
                refreshTokenRepositoryMock.Object,
                passwordHasherMock.Object,
                jwtTokenGeneratorMock.Object,
                refreshTokenGeneratorMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () => await handler.Handle(
                new LoginUserCommand(
                    Email: "test@example.com",
                    Password: "password123"),
                CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();

            passwordHasherMock.Verify(
                hasher => hasher.Verify(
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);

            refreshTokenGeneratorMock.Verify(
                generator => generator.Generate(),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowArgumentException_WhenPasswordIsInvalid()
        {
            var user = User.Create(
                "test@example.com",
                "hashed-password");

            var userRepositoryMock = new Mock<IUserRepository>();
            var refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
            var passwordHasherMock = new Mock<IPasswordHasher>();
            var jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
            var refreshTokenGeneratorMock = new Mock<IRefreshTokenGenerator>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();

            userRepositoryMock
                .Setup(repository => repository.GetByEmailAsync(
                    "test@example.com",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            passwordHasherMock
                .Setup(hasher => hasher.Verify(
                    "wrong-password",
                    user.PasswordHash))
                .Returns(false);

            var handler = new LoginUserCommandHandler(
                userRepositoryMock.Object,
                refreshTokenRepositoryMock.Object,
                passwordHasherMock.Object,
                jwtTokenGeneratorMock.Object,
                refreshTokenGeneratorMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () => await handler.Handle(
                new LoginUserCommand(
                    Email: "test@example.com",
                    Password: "wrong-password"),
                CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();

            refreshTokenGeneratorMock.Verify(
                generator => generator.Generate(),
                Times.Never);

            refreshTokenRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<RefreshToken>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}