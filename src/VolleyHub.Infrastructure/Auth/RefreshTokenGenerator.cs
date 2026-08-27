using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using VolleyHub.Application.Auth.Common;
using VolleyHub.Application.Common.Interfaces;

namespace VolleyHub.Infrastructure.Auth
{
    public sealed class RefreshTokenGenerator : IRefreshTokenGenerator
    {
        private const int TokenSizeInBytes = 64;

        private readonly RefreshTokenOptions _options;
        private readonly IDateTimeProvider _dateTimeProvider;

        public RefreshTokenGenerator(IOptions<RefreshTokenOptions> options, IDateTimeProvider dateTimeProvider)
        {
            _options = options.Value;
            _dateTimeProvider = dateTimeProvider;
        }

        public GeneratedRefreshToken Generate()
        {
            if (_options.ExpirationDays <= 0)
            {
                throw new InvalidOperationException("Refresh token expiration must be greater than zero days.");
            }

            var tokenBytes = RandomNumberGenerator.GetBytes(TokenSizeInBytes);

            var token = Convert
                .ToBase64String(tokenBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            var tokenHash = HashToken(token);
            var expiresAt = _dateTimeProvider.UtcNow.AddDays(_options.ExpirationDays);

            return new GeneratedRefreshToken(
                token,
                tokenHash,
                expiresAt);
        }

        public string HashToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Refresh token is required.", nameof(token));
            }

            var tokenBytes = Encoding.UTF8.GetBytes(token);
            var hashBytes = SHA256.HashData(tokenBytes);

            return Convert.ToHexString(hashBytes);
        }
    }
}