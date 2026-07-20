using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VolleyHub.Infrastructure.Persistence;

namespace VolleyHub.Api.IntegrationTests.Common
{
    public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"VolleyHubTests-{Guid.NewGuid()}";

        public CustomWebApplicationFactory()
        {
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__DefaultConnection",
                "Host=localhost;Port=5432;Database=volleyhub_tests;Username=postgres;Password=postgres");

            Environment.SetEnvironmentVariable("Jwt__Issuer", "VolleyHub");
            Environment.SetEnvironmentVariable("Jwt__Audience", "VolleyHub");
            Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");
            Environment.SetEnvironmentVariable(
                "Jwt__Secret",
                "volleyhub-test-secret-key-change-me-please-1234567890");

            Environment.SetEnvironmentVariable(
                "Cors__AllowedOrigins__0",
                "http://localhost:5173");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });

                var serviceProvider = services.BuildServiceProvider();

                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
            });
        }
    }
}
