using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(o => o.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(o => o.Slug)
            .IsUnique();

        builder.Property(o => o.Description)
            .HasMaxLength(1000);
    }
}
