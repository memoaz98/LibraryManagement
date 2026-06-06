using FluentValidation;
using LibraryManagement.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagement.Application.Common;

/// <summary>
/// Extension methods for wiring the Application layer services and
/// validators into an <see cref="IServiceCollection"/>.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers all application services and FluentValidation validators
    /// discovered in this assembly.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CategoryService>();
        services.AddScoped<BookService>();

        services.AddValidatorsFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);

        return services;
    }
}