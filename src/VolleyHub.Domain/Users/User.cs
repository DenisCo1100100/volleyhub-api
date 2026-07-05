using VolleyHub.Domain.Common;

namespace VolleyHub.Domain.Users
{
    public sealed class User : AuditableEntity
    {
        public const int MaxEmailLength = 320;
        public const int MaxPasswordHashLength = 500;

        private User() { }

        private User(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; private set; }
        public string Email { get; private set; } = null!;
        public string PasswordHash { get; private set; } = null!;

        public static User Create(string email, string passwordHash)
        {
            ValidateEmail(email);
            ValidatePasswordHash(passwordHash);

            return new User(Guid.NewGuid())
            {
                Email = NormalizeEmail(email),
                PasswordHash = passwordHash
            };
        }

        public void ChangePasswordHash(string passwordHash)
        {
            ValidatePasswordHash(passwordHash);

            PasswordHash = passwordHash;
        }

        private static void ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email is required.", nameof(email));
            }

            if (email.Trim().Length > MaxEmailLength)
            {
                throw new ArgumentException(
                    $"Email must be {MaxEmailLength} characters or less.",
                    nameof(email));
            }
        }

        private static void ValidatePasswordHash(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                throw new ArgumentException("Password hash is required.", nameof(passwordHash));
            }

            if (passwordHash.Length > MaxPasswordHashLength)
            {
                throw new ArgumentException(
                    $"Password hash must be {MaxPasswordHashLength} characters or less.",
                    nameof(passwordHash));
            }
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }
    }
}