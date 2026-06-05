using LibraryManagement.Domain.Interfaces;
using LibraryManagement.Infrastructure.Persistence;
using LibraryManagement.Infrastructure.Persistence.Repositories.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagement.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for wiring the LibraryManagement infrastructure
/// services into an <see cref="IServiceCollection"/>.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Entity Framework Core implementations of all
    /// domain repositories, the <c>LibraryDbContext</c>, and the EF Core
    /// flavor of <see cref="IUnitOfWork"/>.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">
    /// The application configuration. Reads the
    /// <c>ConnectionStrings:LibraryDb</c> entry to wire the DbContext.
    /// </param>
    public static IServiceCollection AddEntityFrameworkInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LibraryDb")
            ?? throw new InvalidOperationException(
                "Connection string 'LibraryDb' is missing from configuration.");

        services.AddDbContext<LibraryDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddScoped<ICategoryRepository, EfCategoryRepository>();
        services.AddScoped<IAuthorRepository, EfAuthorRepository>();
        services.AddScoped<IBookRepository, EfBookRepository>();
        services.AddScoped<IBookCopyRepository, EfBookCopyRepository>();
        services.AddScoped<IMemberRepository, EfMemberRepository>();
        services.AddScoped<ILoanRepository, EfLoanRepository>();

        return services;
    }
}