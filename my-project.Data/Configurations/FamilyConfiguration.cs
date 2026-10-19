using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using my_project.Core.Entities;

namespace my_project.Data.Configurations;

public class FamilyConfiguration : IEntityTypeConfiguration<Family>
{
    public void Configure(EntityTypeBuilder<Family> builder)
    {
        builder.HasKey(family => family.Id);

        builder.Property(family => family.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(family => family.JoinCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(family => family.JoinCode)
            .IsUnique();

        builder.HasOne(family => family.CreatedBy)
            .WithMany(user => user.CreatedFamilies)
            .HasForeignKey(family => family.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
