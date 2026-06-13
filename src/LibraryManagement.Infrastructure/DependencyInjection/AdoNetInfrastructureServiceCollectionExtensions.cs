using LibraryManagement.Application.Auth;
using LibraryManagement.Application.Auth.Interfaces;
using LibraryManagement.Domain.Interfaces;
using LibraryManagement.Infrastructure.Auth;
using LibraryManagement.Infrastructure.Persistence;
using LibraryManagement.Infrastructure.Persistence.Repositories.Ado;
using LibraryManagement.Infrastructure.Persistence.Repositories.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagement.Infrastructure.DependencyInjection;

public static class AdoNetInfrastructureServiceCollectionExtensions
{
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

        services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();

        services.Configure<JwtOptions>(opts =>
        {
            configuration.GetSection(JwtOptions.SectionName).Bind(opts);
        });
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}