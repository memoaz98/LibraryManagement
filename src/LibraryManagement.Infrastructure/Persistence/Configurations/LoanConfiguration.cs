using LibraryManagement.Infrastructure.Persistence.DataModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Persistence.Configurations;

public class LoanConfiguration : IEntityTypeConfiguration<LoanDataModel>
{
    public void Configure(EntityTypeBuilder<LoanDataModel> builder)
    {
        builder.ToTable("Loans");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedOnAdd();

        builder.Property(l => l.IsDeleted).HasDefaultValue(false);
        builder.Property(l => l.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(l => l.LoanDate)
               .HasDefaultValueSql("CAST(SYSUTCDATETIME() AS DATE)");

        builder.HasOne(l => l.BookCopy)
               .WithMany(c => c.Loans)
               .HasForeignKey(l => l.BookCopyId)
               .HasConstraintName("FK_Loans_BookCopies_BookCopyId")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Member)
               .WithMany(m => m.Loans)
               .HasForeignKey(l => l.MemberId)
               .HasConstraintName("FK_Loans_Members_MemberId")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Status)
               .WithMany()
               .HasForeignKey(l => l.StatusId)
               .HasConstraintName("FK_Loans_LoanStatus_StatusId")
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.BookCopyId).HasDatabaseName("IX_Loans_BookCopyId");
        builder.HasIndex(l => l.MemberId).HasDatabaseName("IX_Loans_MemberId");
        builder.HasIndex(l => l.StatusId).HasDatabaseName("IX_Loans_StatusId");
        builder.HasIndex(l => l.DueDate)
               .HasDatabaseName("IX_Loans_DueDate")
               .HasFilter("[ReturnDate] IS NULL");
    }
}