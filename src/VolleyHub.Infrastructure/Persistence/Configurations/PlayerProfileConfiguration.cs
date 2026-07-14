using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Infrastructure.Persistence.Configurations
{
    public sealed class PlayerProfileConfiguration : IEntityTypeConfiguration<PlayerProfile>
    {
        public void Configure(EntityTypeBuilder<PlayerProfile> builder)
        {
            builder.ToTable("player_profiles");

            builder.HasKey(playerProfile => playerProfile.Id);

            builder.Property(playerProfile => playerProfile.Id)
                .HasColumnName("id");

            builder.Property(playerProfile => playerProfile.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(playerProfile => playerProfile.DisplayName)
                .HasColumnName("display_name")
                .HasMaxLength(PlayerProfile.MaxDisplayNameLength)
                .IsRequired();

            builder.Property(playerProfile => playerProfile.SkillLevel)
                .HasColumnName("skill_level")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(playerProfile => playerProfile.City)
                .HasColumnName("city")
                .HasMaxLength(PlayerProfile.MaxCityLength);

            builder.Property(playerProfile => playerProfile.Bio)
                .HasColumnName("bio")
                .HasMaxLength(PlayerProfile.MaxBioLength);

            builder.Property(playerProfile => playerProfile.IsDeleted)
                .HasColumnName("is_deleted")
                .IsRequired();

            builder.Property(playerProfile => playerProfile.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(playerProfile => playerProfile.UpdatedAt)
                .HasColumnName("updated_at");

            builder.HasOne<VolleyHub.Domain.Users.User>()
                .WithMany()
                .HasForeignKey(playerProfile => playerProfile.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(playerProfile => playerProfile.UserId)
                .IsUnique();

            builder.HasIndex(playerProfile => playerProfile.DisplayName);

            builder.HasIndex(playerProfile => playerProfile.City);

            builder.HasIndex(playerProfile => playerProfile.SkillLevel);

            builder.HasQueryFilter(playerProfile => !playerProfile.IsDeleted);
        }
    }
}