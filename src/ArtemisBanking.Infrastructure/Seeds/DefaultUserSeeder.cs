using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Domain.Enums;
using ArtemisBanking.Infrastructure.Persistence;
using ArtemisBanking.Shared.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data;

namespace ArtemisBanking.Infrastructure.Seeds;
 
public static class DefaultUserSeeder
{
    public static readonly string[] Roles =
        { "Admin", "Cajero", "Cliente", "Comercio" };

    private static readonly SeedUser[] DefaultUsers =
    {
        new() { FirstName="Admin", LastName="Principal", IdentityCard="000-0000000-1",
                UserName="admin", Email="1000.gerald.manuel@gmail.com", Password="Admin@12345", Role=UserRole.Admin },
        new() { FirstName="Cajero", LastName="Principal", IdentityCard="000-0000000-2",
                UserName="cajero", Email="cajero@artemisbanking.com", Password="Cajero@12345", Role=UserRole.Cajero },
        new() { FirstName="Cliente", LastName="Demo", IdentityCard="000-0000000-3",
                UserName="cliente", Email="cliente@artemisbanking.com", Password="Cliente@12345", Role=UserRole.Cliente, InitialBalance=50000m }
    };

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext   = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger      = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        await ApplyMigrationsAsync(dbContext, logger);
        await SeedRolesAsync(roleManager, logger);
        await SeedUsersAsync(userManager, dbContext, logger);
    }

    private static async Task ApplyMigrationsAsync(AppDbContext dbContext, ILogger logger)
    {
        var dbExists = await DatabaseExistsAsync(dbContext, logger);

        if (!dbExists)
        {
            logger.LogInformation("Inicializando base de datos por primera vez...");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Base de datos creada y migraciones aplicadas.");
            return;
        }

        logger.LogInformation("Base de datos existente detectada. Verificando estado de tablas Identity...");

        // Detecta el escenario de migracion vacia: la migracion esta registrada
        // en __EFMigrationsHistory pero las tablas de Identity no existen porque
        // el metodo Up() estaba vacio. En ese caso, se elimina el registro y se
        // re-aplica la migracion correcta.
        var identityTablesExist = await IdentityTablesExistAsync(dbContext, logger);
        if (!identityTablesExist)
        {
            logger.LogWarning(
                "Las tablas de Identity no existen aunque la migracion ya esta registrada. " +
                "Se detecta migracion vacia anterior. Eliminando registro y re-aplicando...");

            await RemoveMigrationHistoryAsync(dbContext, logger);
        }

        var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();

        if (pending.Count == 0)
        {
            logger.LogInformation("Base de datos al dia. No hay migraciones pendientes.");
            return;
        }

        logger.LogInformation("Aplicando {Count} migracion(es): {Names}",
            pending.Count, string.Join(", ", pending));
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Migraciones aplicadas correctamente.");
    }

    private static async Task<bool> DatabaseExistsAsync(AppDbContext dbContext, ILogger logger)
    {
        var connStr = dbContext.Database.GetConnectionString()
                     ?? dbContext.Database.GetDbConnection().ConnectionString;

        var masterConn = new SqlConnectionStringBuilder(connStr)
            { InitialCatalog = "master" }.ConnectionString;

        var dbName = dbContext.Database.GetDbConnection().Database;

        try
        {
            await using var conn = new SqlConnection(masterConn);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(1) FROM sys.databases WHERE name = @n";
            cmd.Parameters.AddWithValue("@n", dbName);
            return (int)(await cmd.ExecuteScalarAsync())! > 0;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo consultar sys.databases; se asume BD nueva.");
            return false;
        }
    }

    private static async Task<bool> IdentityTablesExistAsync(AppDbContext dbContext, ILogger logger)
    {
        try
        {
            var conn = dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetRoles'";
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo verificar existencia de tablas Identity; se asume que no existen.");
            return false;
        }
    }

    private static async Task RemoveMigrationHistoryAsync(AppDbContext dbContext, ILogger logger)
    {
        try
        {
            var conn = dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM [__EFMigrationsHistory]";
            var rows = await cmd.ExecuteNonQueryAsync();
            logger.LogInformation("Se eliminaron {Rows} registro(s) de __EFMigrationsHistory.", rows);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al limpiar __EFMigrationsHistory.");
            throw;
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Rol '{Role}' creado.", role);
            }
        }
    }

    private static async Task SeedUsersAsync(
        UserManager<ApplicationUser> userManager,
        AppDbContext dbContext, ILogger logger)
    {
        foreach (var seed in DefaultUsers)
        {
            if (await userManager.FindByNameAsync(seed.UserName) != null) continue;

            var user = new ApplicationUser
            {
                FirstName    = seed.FirstName, LastName     = seed.LastName,
                IdentityCard = seed.IdentityCard, UserName  = seed.UserName,
                Email        = seed.Email, EmailConfirmed   = true,
                IsActive     = true, Role                   = seed.Role
            };

            var result = await userManager.CreateAsync(user, seed.Password);
            if (!result.Succeeded)
            {
                // Loggear los errores reales de Identity para facilitar diagnostico
                var errors = string.Join("; ", result.Errors.Select(e => $"[{e.Code}] {e.Description}"));
                logger.LogError("Error creando usuario '{U}': {Errors}", seed.UserName, errors);
                continue;
            }

            await userManager.AddToRoleAsync(user, seed.Role.ToString());
            logger.LogInformation("Usuario '{U}' ({R}) creado exitosamente.", seed.UserName, seed.Role);

            if (seed.Role == UserRole.Cliente)
                await CreateMainAccountAsync(user, seed.InitialBalance, dbContext, logger);
        }
    }

    private static async Task CreateMainAccountAsync(
        ApplicationUser client, decimal balance,
        AppDbContext db, ILogger logger)
    {
        string number;
        do { number = AccountNumberGenerator.Generate9Digits(); }
        while (await db.SavingsAccounts.AnyAsync(s => s.AccountNumber == number)
            || await db.Loans.AnyAsync(l => l.LoanNumber == number));

        db.SavingsAccounts.Add(new SavingsAccount
        {
            AccountNumber = number, Balance = balance,
            AccountType   = AccountType.Main, IsActive = true,
            CreatedAt     = DateTime.UtcNow, ClientId  = client.Id
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Cuenta principal #{N} creada para '{U}'.", number, client.UserName);
    }

    private sealed class SeedUser
    {
        public string FirstName       { get; init; } = string.Empty;
        public string LastName        { get; init; } = string.Empty;
        public string IdentityCard    { get; init; } = string.Empty;
        public string UserName        { get; init; } = string.Empty;
        public string Email           { get; init; } = string.Empty;
        public string Password        { get; init; } = string.Empty;
        public UserRole Role          { get; init; }
        public decimal InitialBalance { get; init; } = 0m;
    }
}
