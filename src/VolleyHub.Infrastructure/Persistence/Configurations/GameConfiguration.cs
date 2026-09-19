using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Infrastructure.Persistence.Configurations
{
    public sealed class GameConfiguration : IEntityTypeConfiguration<Game>
    {
        public void Configure(EntityTypeBuilder<Game> builder)
        {
            builder.ToTable("games");

            builder.HasKey(game => game.Id);

            builder.Property<Guid>("Version")
                .HasColumnName("version")
                .IsConcurrencyToken();

            builder.Property(game => game.Id)
                .HasColumnName("id");

            builder.Property(game => game.OrganizerId)
                .HasColumnName("organizer_id")
                .IsRequired();

            builder.Property(game => game.CourtId)
                .HasColumnName("court_id")
                .IsRequired();

            builder.Property(game => game.StartsAt)
                .HasColumnName("starts_at")
                .IsRequired();

            builder.Property(game => game.EndsAt)
                .HasColumnName("ends_at");

            builder.Property(game => game.MaxPlayers)
                .HasColumnName("max_players")
                .IsRequired();

            builder.Property(game => game.PricePerPlayer)
                .HasColumnName("price_per_player")
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(game => game.RequiredLevel)
                .HasColumnName("required_level")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(game => game.JoinPolicy)
                .HasColumnName("join_policy")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(game => game.Description)
                .HasColumnName("description")
                .HasMaxLength(Game.MaxDescriptionLength);

            builder.Property(game => game.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(game => game.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(game => game.UpdatedAt)
                .HasColumnName("updated_at");

            builder.HasOne<Court>()
                .WithMany()
                .HasForeignKey(game => game.CourtId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<PlayerProfile>()
                .WithMany()
                .HasForeignKey(game => game.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(game => game.OrganizerId);
            builder.HasIndex(game => game.CourtId);
            builder.HasIndex(game => game.StartsAt);
            builder.HasIndex(game => game.Status);
            builder.HasIndex(game => game.RequiredLevel);
            builder.HasIndex(game => game.JoinPolicy);
        }
    }
}
