using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Infrastructure.Persistence.Configurations
{
    public sealed class GameTemplateConfiguration : IEntityTypeConfiguration<GameTemplate>
    {
        public void Configure(EntityTypeBuilder<GameTemplate> builder)
        {
            builder.ToTable("game_templates", table => table.HasCheckConstraint("CK_game_templates_duration", "duration IS NULL OR duration > interval '0 seconds'"));
            builder.HasKey(template => template.Id);
            builder.Property(template => template.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property<Guid>("Version").HasColumnName("version").IsConcurrencyToken();
            builder.Property(template => template.OrganizerId).HasColumnName("organizer_id");
            builder.Property(template => template.Name).HasColumnName("name").HasMaxLength(GameTemplate.MaxNameLength);
            builder.Property(template => template.CourtId).HasColumnName("court_id");
            builder.Property(template => template.Duration).HasColumnName("duration");
            builder.Property(template => template.MaxPlayers).HasColumnName("max_players");
            builder.Property(template => template.PricePerPlayer).HasColumnName("price_per_player").HasPrecision(18, 2);
            builder.Property(template => template.RequiredLevel).HasColumnName("required_level").HasConversion<string>().HasMaxLength(50);
            builder.Property(template => template.JoinPolicy).HasColumnName("join_policy").HasConversion<string>().HasMaxLength(50);
            builder.Property(template => template.Description).HasColumnName("description").HasMaxLength(Game.MaxDescriptionLength);
            builder.Property(template => template.CreatedAt).HasColumnName("created_at");
            builder.Property(template => template.UpdatedAt).HasColumnName("updated_at");
            builder.HasOne<PlayerProfile>().WithMany().HasForeignKey(template => template.OrganizerId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Court>().WithMany().HasForeignKey(template => template.CourtId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
