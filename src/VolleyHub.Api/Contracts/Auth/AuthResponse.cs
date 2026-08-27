namespace VolleyHub.Api.Contracts.Auth
{
    public sealed record AuthResponse(
        Guid UserId,
        string Email,
        string AccessToken);
}