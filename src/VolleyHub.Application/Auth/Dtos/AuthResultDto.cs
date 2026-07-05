namespace VolleyHub.Application.Auth.Dtos
{
    public sealed record AuthResultDto(
        Guid UserId,
        string Email,
        string AccessToken);
}