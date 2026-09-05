using ArtemisBanking.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// ArtemisBanking.Functions Isolated Worker (.NET 8 / Azure Functions v4)

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((ctx, services) =>
    {
        // Infraestructura completa: EF Core, repositorios, email, LoanService
        // El connection string se lee desde local.settings.json ("SqlConnectionString")
        // o desde Azure App Settings en producción.
        services.AddInfrastructure(ctx.Configuration);

        // Application Insights
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

await host.RunAsync();
