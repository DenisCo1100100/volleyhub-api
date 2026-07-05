using System.Security.Claims;
using VolleyHub.Application.Common.Interfaces;

namespace VolleyHub.Api.Services
{
    public sealed class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId
        {
            get
            {
                var userId = _httpContextAccessor.HttpContext?.User
                    .FindFirstValue(ClaimTypes.NameIdentifier);

                return Guid.TryParse(userId, out var parsedUserId)
                    ? parsedUserId
                    : null;
            }
        }
    }
}