using LibraryManagement.Application.Loans;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagement.Infrastructure.DependencyInjection;

internal static class LoanOptionsServiceCollectionExtensions
{
    // The loan duration bounds the Loan entity enforces.
    private const int MinLoanDays = 1;
    private const int MaxLoanDays = 90;

    /// <summary>
    /// Binds the "Loan" section and validates it when the host starts, so a
    /// bad value stops the app instead of failing the first checkout.
    /// </summary>
    public static IServiceCollection AddLoanOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<LoanOptions>()
            .Bind(configuration.GetSection(LoanOptions.SectionName))
            .Validate(
                options => options.DefaultLoanDays is >= MinLoanDays and <= MaxLoanDays,
                $"Loan:DefaultLoanDays must be between {MinLoanDays} and {MaxLoanDays}.")
            .Validate(
                options => options.MaxActiveLoansPerMember >= 1,
                "Loan:MaxActiveLoansPerMember must be at least 1.")
            .ValidateOnStart();

        return services;
    }
}
