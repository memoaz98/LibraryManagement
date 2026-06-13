using LibraryManagement.Application.Common;
using LibraryManagement.Infrastructure.DependencyInjection;
using LibraryManagement.WebApi.ExceptionHandlers;
using Scalar.AspNetCore;
using Serilog;

// Inicializa un logger "bootstrap" — captura errores que pasen ANTES de que
// el host esté completamente configurado (ej. errores al leer appsettings).
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

    // Auth: Identity Core + JWT Bearer middleware.
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

    // Authentication BEFORE authorization. Order matters.
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