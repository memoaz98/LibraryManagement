using LibraryManagement.Application.Auth;
using LibraryManagement.Application.Auth.Interfaces;
using LibraryManagement.Domain.Interfaces;
using LibraryManagement.Infrastructure.Auth;
using LibraryManagement.Infrastructure.Persistence;
using LibraryManagement.Infrastructure.Persistence.Repositories.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagement.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
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

        services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();

        services.Configure<JwtOptions>(opts =>
        {
            configuration.GetSection(JwtOptions.SectionName).Bind(opts);
        });
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}