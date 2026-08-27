namespace VolleyHub.Application.Auth.Common
{
    public sealed record GeneratedRefreshToken(
        string Token,
        string TokenHash,
        DateTimeOffset ExpiresAt);
}