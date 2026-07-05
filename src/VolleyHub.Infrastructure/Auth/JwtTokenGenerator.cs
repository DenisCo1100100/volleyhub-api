using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Users;

namespace VolleyHub.Infrastructure.Auth
{
    public sealed class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtOptions _jwtOptions;

        public JwtTokenGenerator(IOptions<JwtOptions> jwtOptions)
        {
            _jwtOptions = jwtOptions.Value;
        }

        public string GenerateToken(User user)
        {
            if (string.IsNullOrWhiteSpace(_jwtOptions.Secret))
            {
                throw new InvalidOperationException("JWT secret is not configured.");
            }

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtOptions.Secret));

            var signingCredentials = new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes),
                signingCredentials: signingCredentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}