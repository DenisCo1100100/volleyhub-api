using FluentAssertions;
using Moq;
using VolleyHub.Application.Auth.Commands.RegisterUser;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Users;

namespace VolleyHub.Application.UnitTests.Auth.Commands.RegisterUser
{
    public sealed class RegisterUserCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldCreateUserAndReturnAuthResult_WhenCommandIsValid()
        {
            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();
            var passwordHasherMock = new Mock<IPasswordHasher>();
            var jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            User? addedUser = null;

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

            passwordHasherMock
                .Setup(hasher => hasher.Hash("password123"))
                .Returns("hashed-password");

            jwtTokenGeneratorMock
                .Setup(generator => generator.GenerateToken(It.IsAny<User>()))
                .Returns("access-token");

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new RegisterUserCommandHandler(
                userRepositoryMock.Object,
                passwordHasherMock.Object,
                jwtTokenGeneratorMock.Object,
                unitOfWorkMock.Object);

            var command = new RegisterUserCommand(
                Email: "Test@Example.com",
                Password: "password123");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            addedUser.Should().NotBeNull();
            addedUser!.Email.Should().Be("test@example.com");
            addedUser.PasswordHash.Should().Be("hashed-password");

            result.UserId.Should().Be(addedUser.Id);
            result.Email.Should().Be(addedUser.Email);
            result.AccessToken.Should().Be("access-token");

            userRepositoryMock.Verify(
                repository => repository.GetByEmailAsync(
                    "test@example.com",
                    It.IsAny<CancellationToken>()),
                Times.Once);

            userRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<User>(),
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
            // Arrange
            var existingUser = User.Create(
                "test@example.com",
                "hashed-password");

            var userRepositoryMock = new Mock<IUserRepository>();
            var passwordHasherMock = new Mock<IPasswordHasher>();
            var jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            userRepositoryMock
                .Setup(repository => repository.GetByEmailAsync(
                    "test@example.com",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            var handler = new RegisterUserCommandHandler(
                userRepositoryMock.Object,
                passwordHasherMock.Object,
                jwtTokenGeneratorMock.Object,
                unitOfWorkMock.Object);

            var command = new RegisterUserCommand(
                Email: "test@example.com",
                Password: "password123");

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();

            passwordHasherMock.Verify(
                hasher => hasher.Hash(It.IsAny<string>()),
                Times.Never);

            userRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<User>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}