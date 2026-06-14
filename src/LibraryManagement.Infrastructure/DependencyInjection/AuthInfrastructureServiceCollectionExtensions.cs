using System.IdentityModel.Tokens.Jwt;
using System.Text;
using LibraryManagement.Application.Auth;
using LibraryManagement.Domain.Constants;
using LibraryManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace LibraryManagement.Infrastructure.DependencyInjection;

public static class AuthInfrastructureServiceCollectionExtensions
{
    public static class Policies
    {
        public const string AdminOnly = "AdminOnly";
        public const string LibrarianOrAdmin = "LibrarianOrAdmin";
        public const string ReaderOrAbove = "ReaderOrAbove";
    }

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Disable legacy WS-* claim type mapping on JWT inbound side, so the
        // JWT can carry plain "role" claims instead of the long URI.
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        services
            .AddIdentityCore<IdentityUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<LibraryDbContext>()
            .AddDefaultTokenProviders();

        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var jwtOptions = new JwtOptions();
        jwtSection.Bind(jwtOptions);

        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey is missing from configuration. Set it via user secrets or environment variables.");
        }

        var signingKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtOptions.SigningKey));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),

                    RequireExpirationTime = true,
                    RequireSignedTokens = true,

                    RoleClaimType = "role"
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.AdminOnly, p =>
                p.RequireRole(RoleIds.Names.Administrator));

            options.AddPolicy(Policies.LibrarianOrAdmin, p =>
                p.RequireRole(RoleIds.Names.Administrator, RoleIds.Names.Librarian));

            options.AddPolicy(Policies.ReaderOrAbove, p =>
                p.RequireRole(
                    RoleIds.Names.Administrator,
                    RoleIds.Names.Librarian,
                    RoleIds.Names.Reader));
        });

        return services;
    }
}