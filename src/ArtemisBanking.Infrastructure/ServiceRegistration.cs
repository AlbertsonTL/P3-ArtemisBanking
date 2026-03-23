using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Application.Interfaces.Services;
using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Infrastructure.Mappings;
using ArtemisBanking.Infrastructure.Persistence;
using ArtemisBanking.Infrastructure.Repositories;
using ArtemisBanking.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArtemisBanking.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core + SQL Server
        var connStr = configuration.GetConnectionString("DefaultConnection")
                   ?? configuration["SqlConnectionString"]
                   ?? throw new InvalidOperationException(
                       "Connection string not found. Set 'DefaultConnection' (WebApp) or 'SqlConnectionString' (Azure Functions).");

        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlServer(connStr,
                sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // Identity
        var identityBuilder = services.AddIdentity<ApplicationUser, IdentityRole>(opt =>
        {
            opt.Password.RequireDigit           = true;
            opt.Password.RequireLowercase       = true;
            opt.Password.RequireUppercase       = true;
            opt.Password.RequireNonAlphanumeric = true;
            opt.Password.RequiredLength         = 8;
            opt.User.RequireUniqueEmail         = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // AutoMapper
        services.AddAutoMapper(typeof(UserMappingProfile).Assembly);

        // Repositorio genérico
        services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));

        // Email
        services.AddScoped<IEmailService, EmailService>();

        // Servicio de Préstamos
        services.AddScoped<ILoanService, LoanService>();

        // Servicio de Transacciones
        services.AddScoped<ITransactionService, TransactionService>();

        return services;
    }
}