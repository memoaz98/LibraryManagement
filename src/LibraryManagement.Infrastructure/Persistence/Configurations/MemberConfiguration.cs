using LibraryManagement.Infrastructure.Persistence.DataModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Persistence.Configurations;

public class MemberConfiguration : IEntityTypeConfiguration<MemberDataModel>
{
    public void Configure(EntityTypeBuilder<MemberDataModel> builder)
    {
        builder.ToTable("Members");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd();

        builder.Property(m => m.UserId).HasMaxLength(450);
        builder.Property(m => m.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.LastName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Email).HasMaxLength(256).IsRequired();
        builder.Property(m => m.IsActive).HasDefaultValue(true);
        builder.Property(m => m.IsDeleted).HasDefaultValue(false);
        builder.Property(m => m.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(m => m.MembershipDate)
               .HasDefaultValueSql("CAST(SYSUTCDATETIME() AS DATE)");

        builder.HasOne(m => m.User)
               .WithOne()
               .HasForeignKey<MemberDataModel>(m => m.UserId)
               .HasConstraintName("FK_Members_AspNetUsers_UserId")
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => m.Email)
               .IsUnique()
               .HasDatabaseName("UX_Members_Email")
               .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(m => m.UserId)
               .IsUnique()
               .HasDatabaseName("UX_Members_UserId")
               .HasFilter("[UserId] IS NOT NULL AND [IsDeleted] = 0");
    }
}