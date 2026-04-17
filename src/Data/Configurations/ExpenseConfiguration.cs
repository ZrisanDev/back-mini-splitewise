using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using back_api_splitwise.src.Entities;

namespace back_api_splitwise.src.Data.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Description)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.PaidBy)
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .IsRequired();

        builder.Property(e => e.GroupId)
            .IsRequired();

        builder.Property(e => e.IsDeleted)
            .HasDefaultValue(false);

        builder.HasIndex(e => e.GroupId);
        builder.HasIndex(e => e.PaidBy);
        builder.HasIndex(e => e.CreatedAt);

        // Navigation: Expense -> Group (many-to-one)
        builder.HasOne(e => e.Group)
            .WithMany()
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation: Expense -> PaidByUser (many-to-one)
        builder.HasOne(e => e.PaidByUser)
            .WithMany(u => u.PaidExpenses)
            .HasForeignKey(e => e.PaidBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation: Expense -> Splits (one-to-many)
        builder.HasMany(e => e.Splits)
            .WithOne(s => s.Expense)
            .HasForeignKey(s => s.ExpenseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
