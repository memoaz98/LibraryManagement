using LibraryManagement.Infrastructure.Persistence.DataModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Persistence.Configurations;

public class BookCopyConfiguration : IEntityTypeConfiguration<BookCopyDataModel>
{
    public void Configure(EntityTypeBuilder<BookCopyDataModel> builder)
    {
        builder.ToTable("BookCopies");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.InventoryCode).HasMaxLength(20).IsRequired();
        builder.Property(c => c.IsDeleted).HasDefaultValue(false);
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(c => c.Book)
               .WithMany(b => b.Copies)
               .HasForeignKey(c => c.BookId)
               .HasConstraintName("FK_BookCopies_Books_BookId")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Status)
               .WithMany()
               .HasForeignKey(c => c.StatusId)
               .HasConstraintName("FK_BookCopies_CopyStatus_StatusId")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.InventoryCode)
               .IsUnique()
               .HasDatabaseName("UX_BookCopies_InventoryCode")
               .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(c => c.BookId).HasDatabaseName("IX_BookCopies_BookId");
        builder.HasIndex(c => c.StatusId).HasDatabaseName("IX_BookCopies_StatusId");
    }
}