using FluentAssertions;
using VolleyHub.Domain.Users;

namespace VolleyHub.Domain.UnitTests.Users
{
    public sealed class UserTests
    {
        [Fact]
        public void Create_ShouldCreateUser_WhenDataIsValid()
        {
            // Arrange
            var email = "Test@Example.com";
            var passwordHash = "hashed-password";

            // Act
            var user = User.Create(email, passwordHash);

            // Assert
            user.Id.Should().NotBeEmpty();
            user.Email.Should().Be("test@example.com");
            user.PasswordHash.Should().Be(passwordHash);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Create_ShouldThrowArgumentException_WhenEmailIsEmpty(string email)
        {
            // Arrange
            var passwordHash = "hashed-password";

            // Act
            Action act = () => User.Create(email, passwordHash);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenEmailIsTooLong()
        {
            // Arrange
            var email = new string('a', User.MaxEmailLength + 1);
            var passwordHash = "hashed-password";

            // Act
            Action act = () => User.Create(email, passwordHash);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Create_ShouldThrowArgumentException_WhenPasswordHashIsEmpty(string passwordHash)
        {
            // Arrange
            var email = "test@example.com";

            // Act
            Action act = () => User.Create(email, passwordHash);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ChangePasswordHash_ShouldUpdatePasswordHash_WhenHashIsValid()
        {
            // Arrange
            var user = User.Create("test@example.com", "old-hash");

            // Act
            user.ChangePasswordHash("new-hash");

            // Assert
            user.PasswordHash.Should().Be("new-hash");
        }
    }
}