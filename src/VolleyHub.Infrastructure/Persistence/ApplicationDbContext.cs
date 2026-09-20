using Microsoft.EntityFrameworkCore;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Auth;
using VolleyHub.Domain.Common;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;
using VolleyHub.Domain.Users;

namespace VolleyHub.Infrastructure.Persistence
{
    public sealed class ApplicationDbContext : DbContext
    {
        private readonly IDateTimeProvider _dateTimeProvider;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IDateTimeProvider dateTimeProvider)
            : base(options)
        {
            _dateTimeProvider = dateTimeProvider;
        }

        public DbSet<Court> Courts => Set<Court>();
        public DbSet<Game> Games => Set<Game>();
        public DbSet<GameRecurrence> GameRecurrences => Set<GameRecurrence>();
        public DbSet<GameParticipant> GameParticipants => Set<GameParticipant>();
        public DbSet<PlayerProfile> PlayerProfiles => Set<PlayerProfile>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<User> Users => Set<User>();

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await UpdateGameVersionsAsync(cancellationToken);
            UpdateAuditableEntities();

            foreach (var entry in ChangeTracker.Entries<GameRecurrence>().Where(entry => entry.State is EntityState.Added or EntityState.Modified))
            {
                entry.Property<Guid>("Version").CurrentValue = Guid.NewGuid();
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        private async Task UpdateGameVersionsAsync(CancellationToken cancellationToken)
        {
            var changedGameIds = ChangeTracker.Entries<GameParticipant>()
                .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Select(entry => entry.Entity.GameId)
                .Distinct()
                .ToArray();

            // Every participation change must compete on the same game row, even when its status stays unchanged.
            foreach (var gameId in changedGameIds)
            {
                var game = await Games.FindAsync([gameId], cancellationToken);

                if (game is not null)
                {
                    Entry(game).Property<Guid>("Version").CurrentValue = Guid.NewGuid();
                }
            }

            foreach (var entry in ChangeTracker.Entries<Game>().Where(entry => entry.State is EntityState.Added or EntityState.Modified))
            {
                entry.Property<Guid>("Version").CurrentValue = Guid.NewGuid();
            }
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
