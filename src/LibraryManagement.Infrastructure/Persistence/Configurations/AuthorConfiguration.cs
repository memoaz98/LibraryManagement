using LibraryManagement.Infrastructure.Persistence.DataModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Persistence.Configurations;

public class AuthorConfiguration : IEntityTypeConfiguration<AuthorDataModel>
{
    public void Configure(EntityTypeBuilder<AuthorDataModel> builder)
    {
        builder.ToTable("Authors");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd();

        builder.Property(a => a.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.LastName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.IsDeleted).HasDefaultValue(false);
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(a => a.LastName).HasDatabaseName("IX_Authors_LastName");
    }
}