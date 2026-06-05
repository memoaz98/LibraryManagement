using System.Linq.Expressions;
using LibraryManagement.Infrastructure.Persistence.DataModels;
using LibraryManagement.Infrastructure.Persistence.DataModels.Catalogs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Persistence;

/// <summary>
/// EF Core session for the LibraryManagement schema.
/// </summary>
/// <remarks>
/// Inherits from <see cref="IdentityDbContext{IdentityUser}"/> to provide
/// ASP.NET Core Identity tables (AspNetUsers, AspNetRoles, ...) out of the box.
/// Applies all <c>IEntityTypeConfiguration&lt;T&gt;</c> defined in this assembly
/// and installs a global query filter that hides soft-deleted rows from default
/// queries (override with <c>IgnoreQueryFilters()</c> when needed).
/// </remarks>
public class LibraryDbContext : IdentityDbContext<IdentityUser>
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : base(options)
    {
    }

    public DbSet<CategoryDataModel> Categories => Set<CategoryDataModel>();
    public DbSet<AuthorDataModel> Authors => Set<AuthorDataModel>();
    public DbSet<BookDataModel> Books => Set<BookDataModel>();
    public DbSet<BookCopyDataModel> BookCopies => Set<BookCopyDataModel>();
    public DbSet<MemberDataModel> Members => Set<MemberDataModel>();
    public DbSet<LoanDataModel> Loans => Set<LoanDataModel>();
    public DbSet<RefreshTokenDataModel> RefreshTokens => Set<RefreshTokenDataModel>();

    public DbSet<CopyStatusDataModel> CopyStatuses => Set<CopyStatusDataModel>();
    public DbSet<LoanStatusDataModel> LoanStatuses => Set<LoanStatusDataModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);

        ApplySoftDeleteFilter(modelBuilder);
    }

    /// <summary>
    /// Installs a global query filter on every entity that has an
    /// <c>IsDeleted</c> property, so default queries automatically exclude
    /// soft-deleted rows.
    /// </summary>
    private static void ApplySoftDeleteFilter(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var isDeletedProperty = entityType.FindProperty("IsDeleted");
            if (isDeletedProperty is null || isDeletedProperty.ClrType != typeof(bool))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var filter = Expression.Lambda(
                Expression.Equal(
                    Expression.Property(parameter, "IsDeleted"),
                    Expression.Constant(false)),
                parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }
}