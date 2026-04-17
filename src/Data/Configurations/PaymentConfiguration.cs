using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using back_api_splitwise.src.Entities;

namespace back_api_splitwise.src.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.FromUserId)
            .IsRequired();

        builder.Property(p => p.ToUserId)
            .IsRequired();

        builder.Property(p => p.GroupId)
            .IsRequired();

        builder.Property(p => p.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(p => p.Note)
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.HasIndex(p => p.GroupId);
        builder.HasIndex(p => p.FromUserId);
        builder.HasIndex(p => p.ToUserId);
        builder.HasIndex(p => p.CreatedAt);

        // Navigation: Payment -> FromUser (many-to-one)
        builder.HasOne(p => p.FromUser)
            .WithMany(u => u.SentPayments)
            .HasForeignKey(p => p.FromUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation: Payment -> ToUser (many-to-one)
        builder.HasOne(p => p.ToUser)
            .WithMany(u => u.ReceivedPayments)
            .HasForeignKey(p => p.ToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation: Payment -> Group (many-to-one)
        builder.HasOne(p => p.Group)
            .WithMany()
            .HasForeignKey(p => p.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
