using VolleyHub.Application.Auth.Common;

namespace VolleyHub.Application.Common.Interfaces
{
    public interface IRefreshTokenGenerator
    {
        GeneratedRefreshToken Generate();
        string HashToken(string token);
    }
}