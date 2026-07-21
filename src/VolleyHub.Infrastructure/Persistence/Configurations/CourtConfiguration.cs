using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Infrastructure.Persistence.Configurations
{
    public sealed class CourtConfiguration
        : IEntityTypeConfiguration<Court>
    {
        public void Configure(
            EntityTypeBuilder<Court> builder)
        {
            builder.ToTable("courts");

            builder.HasKey(court => court.Id);

            builder.Property(court => court.Id)
                .HasColumnName("id");

            builder.Property(court => court.OwnerPlayerProfileId)
                .HasColumnName("owner_player_profile_id");

            builder.Property(court => court.Name)
                .HasColumnName("name")
                .HasMaxLength(Court.MaxNameLength)
                .IsRequired();

            builder.Property(court => court.Address)
                .HasColumnName("address")
                .HasMaxLength(Court.MaxAddressLength)
                .IsRequired();

            builder.Property(court => court.Latitude)
                .HasColumnName("latitude")
                .IsRequired();

            builder.Property(court => court.Longitude)
                .HasColumnName("longitude")
                .IsRequired();

            builder.Property(court => court.SurfaceType)
                .HasColumnName("surface_type")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(court => court.IsIndoor)
                .HasColumnName("is_indoor")
                .IsRequired();

            builder.Property(court => court.Description)
                .HasColumnName("description")
                .HasMaxLength(Court.MaxDescriptionLength);

            builder.Property(court => court.IsDeleted)
                .HasColumnName("is_deleted")
                .IsRequired();

            builder.Property(court => court.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(court => court.UpdatedAt)
                .HasColumnName("updated_at");

            builder.HasOne<PlayerProfile>()
                .WithMany()
                .HasForeignKey(court => court.OwnerPlayerProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(
                court => !court.IsDeleted);

            builder.HasIndex(
                court => court.Name);
        }
    }
}