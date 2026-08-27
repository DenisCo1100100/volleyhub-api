using Microsoft.AspNetCore.Http;

namespace VolleyHub.Api.Options
{
    public sealed class RefreshTokenCookieOptions
    {
        public const string SectionName = "RefreshTokenCookie";

        public string Name { get; init; } = "volleyhub.refreshToken";
        public bool Secure { get; init; } = true;
        public SameSiteMode SameSite { get; init; } = SameSiteMode.Lax;
    }
}