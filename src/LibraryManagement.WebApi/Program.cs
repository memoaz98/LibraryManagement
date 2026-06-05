using LibraryManagement.Infrastructure.DependencyInjection;
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

    // Reemplaza el logger por defecto con Serilog, leyendo configuracion de appsettings.
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Services del contenedor DI.
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    // Infrastructure: switch entre EF y ADO segun appsettings.
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

    var app = builder.Build();

    // Pipeline HTTP.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        // Scalar UI montada en /scalar/v1
        app.MapScalarApiReference();
    }

    // Logueo automatico de cada request HTTP (metodo, ruta, status, duracion).
    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();
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