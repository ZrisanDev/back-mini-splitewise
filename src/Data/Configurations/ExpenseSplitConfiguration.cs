using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using back_api_splitwise.src.Entities;

namespace back_api_splitwise.src.Data.Configurations;

public class ExpenseSplitConfiguration : IEntityTypeConfiguration<ExpenseSplit>
{
    public void Configure(EntityTypeBuilder<ExpenseSplit> builder)
    {
        builder.ToTable("ExpenseSplits");

        builder.HasKey(es => es.Id);

        builder.Property(es => es.ExpenseId)
            .IsRequired();

        builder.Property(es => es.UserId)
            .IsRequired();

        builder.Property(es => es.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(es => es.IsSettled)
            .HasDefaultValue(false);

        builder.HasIndex(es => es.ExpenseId);
        builder.HasIndex(es => es.UserId);

        // Navigation: ExpenseSplit -> Expense (many-to-one)
        builder.HasOne(es => es.Expense)
            .WithMany(e => e.Splits)
            .HasForeignKey(es => es.ExpenseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation: ExpenseSplit -> User (many-to-one)
        builder.HasOne(es => es.User)
            .WithMany(u => u.ExpenseSplits)
            .HasForeignKey(es => es.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
