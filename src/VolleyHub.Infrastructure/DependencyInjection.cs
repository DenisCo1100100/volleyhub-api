using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Infrastructure.Persistence;
using VolleyHub.Infrastructure.Persistence.Repositories;
using VolleyHub.Infrastructure.Services;
using VolleyHub.Infrastructure.Auth;

namespace VolleyHub.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' is not configured.");
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });

            services.Configure<JwtOptions>(
                configuration.GetSection(JwtOptions.SectionName));

            services.AddScoped<ICourtRepository, CourtRepository>();
            services.AddScoped<IGameRepository, GameRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

            return services;
        }
    }
}