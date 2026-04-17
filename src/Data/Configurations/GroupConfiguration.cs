using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using back_api_splitwise.src.Entities;

namespace back_api_splitwise.src.Data.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("Groups");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(g => g.CreatedBy)
            .IsRequired();

        builder.Property(g => g.IsDeleted)
            .HasDefaultValue(false);

        builder.HasIndex(g => g.CreatedBy);
        builder.HasIndex(g => g.CreatedAt);

        // Note: Navigation relationship Group -> GroupUsers is configured from
        // the dependent side in GroupUserConfiguration.
    }
}
