using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Infrastructure.Persistence.Configurations
{
    public sealed class GameRecurrenceConfiguration : IEntityTypeConfiguration<GameRecurrence>
    {
        public void Configure(EntityTypeBuilder<GameRecurrence> builder)
        {
            builder.ToTable("game_recurrences", table => table.HasCheckConstraint("CK_game_recurrences_occurrence_count", "occurrence_count BETWEEN 2 AND 52"));
            builder.HasKey(recurrence => recurrence.Id);
            builder.Property(recurrence => recurrence.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property<Guid>("Version").HasColumnName("version").IsConcurrencyToken();
            builder.Property(recurrence => recurrence.SourceGameId).HasColumnName("source_game_id");
            builder.Property(recurrence => recurrence.OrganizerId).HasColumnName("organizer_id");
            builder.Property(recurrence => recurrence.CourtId).HasColumnName("court_id");
            builder.Property(recurrence => recurrence.FirstStartsAt).HasColumnName("first_starts_at");
            builder.Property(recurrence => recurrence.Duration).HasColumnName("duration");
            builder.Property(recurrence => recurrence.OccurrenceCount).HasColumnName("occurrence_count");
            builder.Property(recurrence => recurrence.MaxPlayers).HasColumnName("max_players");
            builder.Property(recurrence => recurrence.PricePerPlayer).HasColumnName("price_per_player").HasPrecision(18, 2);
            builder.Property(recurrence => recurrence.RequiredLevel).HasColumnName("required_level").HasConversion<string>().HasMaxLength(50);
            builder.Property(recurrence => recurrence.JoinPolicy).HasColumnName("join_policy").HasConversion<string>().HasMaxLength(50);
            builder.Property(recurrence => recurrence.Description).HasColumnName("description").HasMaxLength(Game.MaxDescriptionLength);
            builder.Property(recurrence => recurrence.CancelledAt).HasColumnName("cancelled_at");
            builder.Property(recurrence => recurrence.CreatedAt).HasColumnName("created_at");
            builder.Property(recurrence => recurrence.UpdatedAt).HasColumnName("updated_at");
            builder.HasOne<Game>().WithMany().HasForeignKey(recurrence => recurrence.SourceGameId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<PlayerProfile>().WithMany().HasForeignKey(recurrence => recurrence.OrganizerId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Court>().WithMany().HasForeignKey(recurrence => recurrence.CourtId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
