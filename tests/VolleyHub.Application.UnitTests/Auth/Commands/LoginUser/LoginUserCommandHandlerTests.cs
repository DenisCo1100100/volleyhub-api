using FluentAssertions;
using Moq;
using VolleyHub.Application.Auth.Commands.LoginUser;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Users;

namespace VolleyHub.Application.UnitTests.Auth.Commands.LoginUser
{
    public sealed class LoginUserCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldReturnAuthResult_WhenCredentialsAreValid()
        {
            // Arrange
            var user = User.Create(
                "test@example.com",
                "hashed-password");

            var userRepositoryMock = new Mock<IUserRepository>();
            var passwordHasherMock = new Mock<IPasswordHasher>();
            var jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();

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

            var handler = new LoginUserCommandHandler(
                userRepositoryMock.Object,
                passwordHasherMock.Object,
                jwtTokenGeneratorMock.Object);

            var command = new LoginUserCommand(
                Email: "Test@Example.com",
                Password: "password123");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.UserId.Should().Be(user.Id);
            result.Email.Should().Be(user.Email);
            result.AccessToken.Should().Be("access-token");

            userRepositoryMock.Verify(
                repository => repository.GetByEmailAsync(
                    "test@example.com",
                    It.IsAny<CancellationToken>()),
                Times.Once);

            passwordHasherMock.Verify(
                hasher => hasher.Verify(
                    "password123",
                    user.PasswordHash),
                Times.Once);

            jwtTokenGeneratorMock.Verify(
                generator => generator.GenerateToken(user),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowArgumentException_WhenUserDoesNotExist()
        {
            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();
            var passwordHasherMock = new Mock<IPasswordHasher>();
            var jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();

            userRepositoryMock
                .Setup(repository => repository.GetByEmailAsync(
                    "test@example.com",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            var handler = new LoginUserCommandHandler(
                userRepositoryMock.Object,
                passwordHasherMock.Object,
                jwtTokenGeneratorMock.Object);

            var command = new LoginUserCommand(
                Email: "test@example.com",
                Password: "password123");

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();

            passwordHasherMock.Verify(
                hasher => hasher.Verify(
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);

            jwtTokenGeneratorMock.Verify(
                generator => generator.GenerateToken(It.IsAny<User>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowArgumentException_WhenPasswordIsInvalid()
        {
            // Arrange
            var user = User.Create(
                "test@example.com",
                "hashed-password");

            var userRepositoryMock = new Mock<IUserRepository>();
            var passwordHasherMock = new Mock<IPasswordHasher>();
            var jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();

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
                passwordHasherMock.Object,
                jwtTokenGeneratorMock.Object);

            var command = new LoginUserCommand(
                Email: "test@example.com",
                Password: "wrong-password");

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();

            jwtTokenGeneratorMock.Verify(
                generator => generator.GenerateToken(It.IsAny<User>()),
                Times.Never);
        }
    }
}