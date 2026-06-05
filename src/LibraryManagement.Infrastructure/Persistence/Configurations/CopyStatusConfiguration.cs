using LibraryManagement.Infrastructure.Persistence.DataModels.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Persistence.Configurations;

public class CopyStatusConfiguration : IEntityTypeConfiguration<CopyStatusDataModel>
{
    public void Configure(EntityTypeBuilder<CopyStatusDataModel> builder)
    {
        builder.ToTable("CopyStatus");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name).HasMaxLength(50).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(255);

        builder.HasIndex(s => s.Name).IsUnique().HasDatabaseName("UX_CopyStatus_Name");
    }
}