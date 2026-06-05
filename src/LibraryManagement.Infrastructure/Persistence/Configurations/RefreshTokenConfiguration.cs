using LibraryManagement.Infrastructure.Persistence.DataModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshTokenDataModel>
{
    public void Configure(EntityTypeBuilder<RefreshTokenDataModel> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();

        builder.Property(t => t.UserId).HasMaxLength(450).IsRequired();
        builder.Property(t => t.TokenHash).HasColumnType("varbinary(32)").IsRequired();
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(t => t.User)
               .WithMany()
               .HasForeignKey(t => t.UserId)
               .HasConstraintName("FK_RefreshTokens_AspNetUsers_UserId")
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.TokenHash)
               .IsUnique()
               .HasDatabaseName("UX_RefreshTokens_TokenHash");

        builder.HasIndex(t => t.UserId)
               .HasDatabaseName("IX_RefreshTokens_UserId");
    }
}