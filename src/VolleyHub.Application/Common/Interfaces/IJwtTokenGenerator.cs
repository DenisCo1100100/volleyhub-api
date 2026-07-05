using VolleyHub.Domain.Users;

namespace VolleyHub.Application.Common.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user);
    }
}