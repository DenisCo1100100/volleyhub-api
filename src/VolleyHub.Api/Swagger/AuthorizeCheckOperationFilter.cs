using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace VolleyHub.Api.Swagger
{
    public sealed class AuthorizeCheckOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasAuthorize = context.MethodInfo.DeclaringType?
                .GetCustomAttributes(inherit: true)
                .OfType<AuthorizeAttribute>()
                .Any() == true
                || context.MethodInfo
                    .GetCustomAttributes(inherit: true)
                    .OfType<AuthorizeAttribute>()
                    .Any();

            var hasAllowAnonymous = context.MethodInfo.DeclaringType?
                .GetCustomAttributes(inherit: true)
                .OfType<AllowAnonymousAttribute>()
                .Any() == true
                || context.MethodInfo
                    .GetCustomAttributes(inherit: true)
                    .OfType<AllowAnonymousAttribute>()
                    .Any();

            if (!hasAuthorize || hasAllowAnonymous)
            {
                return;
            }

            operation.Responses.TryAdd(
                StatusCodes.Status401Unauthorized.ToString(),
                new OpenApiResponse { Description = "Unauthorized" });

            operation.Responses.TryAdd(
                StatusCodes.Status403Forbidden.ToString(),
                new OpenApiResponse { Description = "Forbidden" });

            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer")] = new List<string>()
                }
            };
        }
    }
}