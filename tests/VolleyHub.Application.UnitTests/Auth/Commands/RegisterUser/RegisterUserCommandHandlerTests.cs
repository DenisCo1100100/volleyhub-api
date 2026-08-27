using FluentAssertions;
using Moq;
using VolleyHub.Application.Auth.Commands.RegisterUser;
using VolleyHub.Application.Auth.Common;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Auth;
using VolleyHub.Domain.Users;

namespace VolleyHub.Application.UnitTests.Auth.Commands.RegisterUser
{
    public sealed class RegisterUserCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldCreateUserRefreshSessionAndReturnAuthResult_WhenCommandIsValid()
        {
            var refreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(30);

            var userRepositoryMock = new Mock<IUserRepository>();
            var refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
            var passwordHasherMock = new Mock<IPasswordHasher>();
            var jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
            var refreshTokenGeneratorMock = new Mock<IRefreshTokenGenerator>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();

            User? addedUser = null;
            RefreshToken? addedRefreshToken = null;

            userRepositoryMock
                .Setup(repository => repository.GetByEmailAsync(
                    "test@example.com",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            userRepositoryMock
                .Setup(repository => repository.AddAsync(
                    It.IsAny<User>(),
                    It.IsAny<CancellationToken>()))
                .Callback<User, CancellationToken>((user, _) => addedUser = user)
                .Returns(Task.CompletedTask);

            refreshTokenRepositoryMock
                .Setup(repository => repository.AddAsync(
                    It.IsAny<RefreshToken>(),
                    It.IsAny<CancellationToken>()))
                .Callback<RefreshToken, CancellationToken>((refreshToken, _) => addedRefreshToken = refreshToken)
                .Returns(Task.CompletedTask);

            passwordHasherMock
                .Setup(hasher => hasher.Hash("password123"))
                .Returns("hashed-password");

            jwtTokenGeneratorMock
                .Setup(generator => generator.GenerateToken(It.IsAny<User>()))
                .Returns("access-token");

            refreshTokenGeneratorMock
                .Setup(generator => generator.Generate())
                .Returns(new GeneratedRefreshToken(
                    "refresh-token",
                    "refresh-token-hash",
                    refreshTokenExpiresAt));

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            dateTimeProviderMock
                .Setup(provider => provider.UtcNow)
                .Returns(DateTimeOffset.UtcNow);

            var handler = new RegisterUserCommandHandler(
                userRepositoryMock.Object,
                refreshTokenRepositoryMock.Object,
                passwordHasherMock.Object,
                jwtTokenGeneratorMock.Object,
                refreshTokenGeneratorMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var result = await handler.Handle(
                new RegisterUserCommand(
                    Email: "Test@Example.com",
                    Password: "password123"),
                CancellationToken.None);

            addedUser.Should().NotBeNull();
            addedUser!.Email.Should().Be("test@example.com");
            addedUser.PasswordHash.Should().Be("hashed-password");

            addedRefreshToken.Should().NotBeNull();
            addedRefreshToken!.UserId.Should().Be(addedUser.Id);
            addedRefreshToken.TokenHash.Should().Be("refresh-token-hash");
            addedRefreshToken.ExpiresAt.Should().Be(refreshTokenExpiresAt);

            result.UserId.Should().Be(addedUser.Id);
            result.Email.Should().Be(addedUser.Email);
            result.AccessToken.Should().Be("access-token");
            result.RefreshToken.Should().Be("refresh-token");
            result.RefreshTokenExpiresAt.Should().Be(refreshTokenExpiresAt);

            userRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<User>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

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
        public async Task Handle_ShouldThrowArgumentException_WhenEmailAlreadyExists()
        {
            var existingUser = User.Create(
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
                .ReturnsAsync(existingUser);

            var handler = new RegisterUserCommandHandler(
                userRepositoryMock.Object,
                refreshTokenRepositoryMock.Object,
                passwordHasherMock.Object,
                jwtTokenGeneratorMock.Object,
                refreshTokenGeneratorMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () => await handler.Handle(
                new RegisterUserCommand(
                    Email: "test@example.com",
                    Password: "password123"),
                CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();

            passwordHasherMock.Verify(
                hasher => hasher.Hash(It.IsAny<string>()),
                Times.Never);

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