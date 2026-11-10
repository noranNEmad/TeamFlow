using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public class OrganizationMemberConfiguration : IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> builder)
    {
        builder.ToTable("organization_members");

        builder.HasKey(om => new { om.OrganizationId, om.UserId });

        builder.HasOne(om => om.Organization)
            .WithMany(o => o.Members)
            .HasForeignKey(om => om.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(om => om.User)
            .WithMany(u => u.OrganizationMemberships)
            .HasForeignKey(om => om.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(om => om.Role)
            .IsRequired()
            .HasConversion<int>();
    }
}

