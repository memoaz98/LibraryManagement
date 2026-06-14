using LibraryManagement.Application.Common;
using LibraryManagement.Infrastructure.DependencyInjection;
using LibraryManagement.WebApi.ExceptionHandlers;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting LibraryManagement.WebApi");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // CORS for Blazor client.
    const string BlazorClientPolicy = "BlazorClient";
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? Array.Empty<string>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(BlazorClientPolicy, policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    builder.Services.AddApplication();

    var dataAccessProvider = builder.Configuration["DataAccess:Provider"]
        ?? throw new InvalidOperationException(
            "Configuration key 'DataAccess:Provider' is missing. Set it to 'EntityFramework' or 'AdoNet'.");

    Log.Information("Data access provider: {Provider}", dataAccessProvider);

    if (string.Equals(dataAccessProvider, "EntityFramework", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddEntityFrameworkInfrastructure(builder.Configuration);
    }
    else if (string.Equals(dataAccessProvider, "AdoNet", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddAdoNetInfrastructure(builder.Configuration);
    }
    else
    {
        throw new InvalidOperationException(
            $"Unknown DataAccess provider '{dataAccessProvider}'. Expected 'EntityFramework' or 'AdoNet'.");
    }

    builder.Services.AddJwtAuthentication(builder.Configuration);

    var app = builder.Build();

    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();

    app.UseCors(BlazorClientPolicy);

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "LibraryManagement.WebApi terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}