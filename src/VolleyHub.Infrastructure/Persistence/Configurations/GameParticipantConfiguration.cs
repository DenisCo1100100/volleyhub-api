using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolleyHub.Domain.Games;

namespace VolleyHub.Infrastructure.Persistence.Configurations
{
    public sealed class GameParticipantConfiguration : IEntityTypeConfiguration<GameParticipant>
    {
        public void Configure(EntityTypeBuilder<GameParticipant> builder)
        {
            builder.ToTable("game_participants");

            builder.HasKey(participant => participant.Id);

            builder.Property(participant => participant.Id)
                .HasColumnName("id");

            builder.Property(participant => participant.GameId)
                .HasColumnName("game_id")
                .IsRequired();

            builder.Property(participant => participant.PlayerProfileId)
                .HasColumnName("player_profile_id")
                .IsRequired();

            builder.Property(participant => participant.JoinedAt)
                .HasColumnName("joined_at")
                .IsRequired();

            builder.Property(participant => participant.ApprovedAt)
                .HasColumnName("approved_at");

            builder.Property(participant => participant.JoinStatus)
                .HasColumnName("join_status")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(participant => participant.AttendanceStatus)
                .HasColumnName("attendance_status")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(participant => participant.OfflinePaymentStatus)
                .HasColumnName("offline_payment_status")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(participant => participant.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(participant => participant.UpdatedAt)
                .HasColumnName("updated_at");

            builder.HasOne<Game>()
                .WithMany()
                .HasForeignKey(participant => participant.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(participant => participant.GameId);

            builder.HasIndex(participant => participant.PlayerProfileId);

            builder.HasIndex(participant => participant.JoinStatus);

            builder.HasIndex(participant => new
            {
                participant.GameId,
                participant.PlayerProfileId
            })
            .IsUnique();
        }
    }
}