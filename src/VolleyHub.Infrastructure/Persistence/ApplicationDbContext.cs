using Microsoft.EntityFrameworkCore;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Common;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.Users;

namespace VolleyHub.Infrastructure.Persistence
{
    public sealed class ApplicationDbContext : DbContext
    {
        private readonly IDateTimeProvider _dateTimeProvider;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IDateTimeProvider dateTimeProvider)
            : base(options)
        {
            _dateTimeProvider = dateTimeProvider;
        }

        public DbSet<Court> Courts => Set<Court>();
        public DbSet<Game> Games => Set<Game>();
        public DbSet<User> Users => Set<User>();

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditableEntities();

            return base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }

        private void UpdateAuditableEntities()
        {
            var now = _dateTimeProvider.UtcNow;

            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.MarkCreated(now);
                }

                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.MarkUpdated(now);
                }
            }
        }
    }
}