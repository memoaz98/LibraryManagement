using LibraryManagement.Domain.Interfaces;
using LibraryManagement.Infrastructure.Persistence;
using LibraryManagement.Infrastructure.Persistence.Repositories.Ado;
using LibraryManagement.Infrastructure.Persistence.Repositories.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagement.Infrastructure.DependencyInjection;

public static class AdoNetInfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers the ADO.NET implementations for the repositories that have
    /// an ADO version (<c>Category</c>, <c>Book</c>) and falls back to
    /// Entity Framework for the rest. <see cref="IUnitOfWork"/> is wired to
    /// the ADO.NET implementation so the transaction is shared correctly
    /// across all repositories within the request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is an intentional mixed setup: the project demonstrates ADO.NET
    /// usage on two domain entities to illustrate the pattern (connection,
    /// command, parameters, manual mapping, transaction sharing) without
    /// duplicating that code six times. The remaining four repositories
    /// keep their EF Core implementation, which still cooperates correctly
    /// because they receive the same DbContext from DI.
    /// </para>
    /// <para>
    /// <strong>Caveat</strong>: in this mixed mode, EF Core writes are NOT
    /// part of the ADO.NET transaction. For a full Unit of Work you would
    /// either implement all six repositories in ADO.NET, or have EF Core
    /// enroll its commands into the same connection/transaction. This is
    /// acceptable for the educational scope; production systems pick one
    /// data access strategy or implement a single coordinator.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddAdoNetInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LibraryDb")
            ?? throw new InvalidOperationException(
                "Connection string 'LibraryDb' is missing from configuration.");

        services.AddDbContext<LibraryDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork, AdoUnitOfWork>();

        services.AddScoped<ICategoryRepository, AdoCategoryRepository>();
        services.AddScoped<IBookRepository, AdoBookRepository>();

        services.AddScoped<IAuthorRepository, EfAuthorRepository>();
        services.AddScoped<IBookCopyRepository, EfBookCopyRepository>();
        services.AddScoped<IMemberRepository, EfMemberRepository>();
        services.AddScoped<ILoanRepository, EfLoanRepository>();

        return services;
    }
}