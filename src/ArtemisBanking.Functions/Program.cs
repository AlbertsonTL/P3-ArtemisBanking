using ArtemisBanking.Infrastructure;
using ArtemisBanking.Infrastructure.Persistence;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// ArtemisBanking.Functions Isolated Worker (.NET 8 / Azure Functions v4)

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((ctx, services) =>
    {
        // EF Core (mismo connection string que la app principal)
        var connectionString = ctx.Configuration["SqlConnectionString"]
            ?? throw new InvalidOperationException(
                "Missing 'SqlConnectionString' in local.settings.json / Azure App Settings.");

        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlServer(connectionString,
                sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // Infraestructura (repositorios, email, LoanService)         
        services.AddInfrastructure(ctx.Configuration);

        // Application Insights
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

await host.RunAsync();
