using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolleyHub.Domain.Games;

namespace VolleyHub.Infrastructure.Persistence.Configurations
{
    public sealed class GameConfiguration : IEntityTypeConfiguration<Game>
    {
        public void Configure(EntityTypeBuilder<Game> builder)
        {
            builder.ToTable("games");

            builder.HasKey(game => game.Id);

            builder.Property(game => game.Id)
                .HasColumnName("id");

            builder.Property(game => game.CourtId)
                .HasColumnName("court_id")
                .IsRequired();

            builder.Property(game => game.StartsAt)
                .HasColumnName("starts_at")
                .IsRequired();

            builder.Property(game => game.MaxPlayers)
                .HasColumnName("max_players")
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

            builder.HasIndex(game => game.CourtId);
            builder.HasIndex(game => game.StartsAt);
            builder.HasIndex(game => game.Status);
        }
    }
}