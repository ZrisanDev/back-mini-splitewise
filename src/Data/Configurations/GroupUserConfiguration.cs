using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using back_api_splitwise.src.Entities;

namespace back_api_splitwise.src.Data.Configurations;

public class GroupUserConfiguration : IEntityTypeConfiguration<GroupUser>
{
    public void Configure(EntityTypeBuilder<GroupUser> builder)
    {
        builder.ToTable("GroupUsers");

        builder.HasKey(gu => gu.Id);

        builder.Property(gu => gu.UserId)
            .IsRequired();

        builder.Property(gu => gu.GroupId)
            .IsRequired();

        builder.Property(gu => gu.Role)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(gu => gu.JoinedAt)
            .IsRequired();

        builder.Property(gu => gu.InvitedBy)
            .IsRequired(false);

        // Composite unique index: a user can only be in a group once
        builder.HasIndex(gu => new { gu.UserId, gu.GroupId })
            .IsUnique();

        builder.HasIndex(gu => gu.UserId);
        builder.HasIndex(gu => gu.GroupId);

        // Navigation properties
        builder.HasOne(gu => gu.User)
            .WithMany(u => u.GroupUsers)
            .HasForeignKey(gu => gu.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(gu => gu.Group)
            .WithMany(g => g.GroupUsers)
            .HasForeignKey(gu => gu.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
