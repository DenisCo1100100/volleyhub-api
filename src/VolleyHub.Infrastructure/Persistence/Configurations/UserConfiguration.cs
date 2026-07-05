using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolleyHub.Domain.Users;

namespace VolleyHub.Infrastructure.Persistence.Configurations
{
    public sealed class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.HasKey(user => user.Id);

            builder.Property(user => user.Id)
                .HasColumnName("id");

            builder.Property(user => user.Email)
                .HasColumnName("email")
                .HasMaxLength(User.MaxEmailLength)
                .IsRequired();

            builder.Property(user => user.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(User.MaxPasswordHashLength)
                .IsRequired();

            builder.Property(user => user.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(user => user.UpdatedAt)
                .HasColumnName("updated_at");

            builder.HasIndex(user => user.Email)
                .IsUnique();
        }
    }
}