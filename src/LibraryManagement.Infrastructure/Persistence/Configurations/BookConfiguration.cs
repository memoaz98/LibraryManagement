using LibraryManagement.Infrastructure.Persistence.DataModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Persistence.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<BookDataModel>
{
    public void Configure(EntityTypeBuilder<BookDataModel> builder)
    {
        builder.ToTable("Books");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedOnAdd();

        builder.Property(b => b.Title).HasMaxLength(255).IsRequired();
        builder.Property(b => b.Isbn).HasColumnName("ISBN").HasMaxLength(13).IsRequired();
        builder.Property(b => b.PublicationYear).IsRequired();
        builder.Property(b => b.IsDeleted).HasDefaultValue(false);
        builder.Property(b => b.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        // FK to Category
        builder.HasOne(b => b.Category)
               .WithMany(c => c.Books)
               .HasForeignKey(b => b.CategoryId)
               .HasConstraintName("FK_Books_Categories_CategoryId")
               .OnDelete(DeleteBehavior.Restrict);

        // N:N with Author (via implicit BookAuthors table)
        builder.HasMany(b => b.Authors)
               .WithMany(a => a.Books)
               .UsingEntity(j => j.ToTable("BookAuthors"));

        // Filtered unique index for ISBN (only on non-deleted rows)
        builder.HasIndex(b => b.Isbn)
               .IsUnique()
               .HasDatabaseName("UX_Books_ISBN")
               .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(b => b.CategoryId).HasDatabaseName("IX_Books_CategoryId");
        builder.HasIndex(b => b.Title).HasDatabaseName("IX_Books_Title");
    }
}