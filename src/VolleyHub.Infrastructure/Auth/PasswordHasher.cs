using Microsoft.AspNetCore.Identity;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Users;

namespace VolleyHub.Infrastructure.Auth
{
    public sealed class PasswordHasher : IPasswordHasher
    {
        private readonly PasswordHasher<User> _passwordHasher = new();

        public string Hash(string password)
        {
            return _passwordHasher.HashPassword(
                user: null!,
                password);
        }

        public bool Verify(
            string password,
            string passwordHash)
        {
            var result = _passwordHasher.VerifyHashedPassword(
                user: null!,
                hashedPassword: passwordHash,
                providedPassword: password);

            return result is PasswordVerificationResult.Success
                or PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}