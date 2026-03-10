using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Domain.Enums;
using ArtemisBanking.Infrastructure.Persistence;
using ArtemisBanking.Shared.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArtemisBanking.Infrastructure.Seeds;

public static class DefaultUserSeeder
{
    public static readonly string[] Roles =
        { "Admin", "Cajero", "Cliente", "Comercio" };

    private static readonly SeedUser[] DefaultUsers =
    {
        new() { FirstName="Admin",   LastName="Principal", IdentityCard="00000000001",
                UserName="admin",   Email="admin@artemisbanking.com",   Password="Admin@12345",   Role=UserRole.Admin   },
        new() { FirstName="Cajero",  LastName="Principal", IdentityCard="00000000002",
                UserName="cajero",  Email="cajero@artemisbanking.com",  Password="Cajero@12345",  Role=UserRole.Cajero  },
        new() { FirstName="Cliente", LastName="Demo",      IdentityCard="00000000003",
                UserName="cliente", Email="cliente@artemisbanking.com", Password="Cliente@12345", Role=UserRole.Cliente,
                InitialBalance=0m }
    };

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        await dbContext.Database.MigrateAsync();
        await SeedRolesAsync(roleManager, logger);
        await SeedUsersAsync(userManager, dbContext, logger);
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
                FirstName = seed.FirstName, LastName = seed.LastName,
                IdentityCard = seed.IdentityCard, UserName = seed.UserName,
                Email = seed.Email, EmailConfirmed = true,
                IsActive = true, Role = seed.Role
            };

            var result = await userManager.CreateAsync(user, seed.Password);
            if (!result.Succeeded) { logger.LogError("Error creando {U}", seed.UserName); continue; }

            await userManager.AddToRoleAsync(user, seed.Role.ToString());
            logger.LogInformation("Usuario '{U}' ({R}) creado.", seed.UserName, seed.Role);

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
            AccountType = AccountType.Main, IsActive = true,
            CreatedAt = DateTime.UtcNow, ClientId = client.Id
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Cuenta principal #{N} creada para '{U}'.", number, client.UserName);
    }

    private sealed class SeedUser
    {
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string IdentityCar { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public UserRole Role { get; init; }
        public decimal InitialBalance { get; init; } = 0m;
    }
}