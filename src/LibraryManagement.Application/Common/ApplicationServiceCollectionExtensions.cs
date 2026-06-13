using FluentValidation;
using LibraryManagement.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagement.Application.Common;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CategoryService>();
        services.AddScoped<BookService>();
        services.AddScoped<AuthService>();

        services.AddValidatorsFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);

        return services;
    }
}