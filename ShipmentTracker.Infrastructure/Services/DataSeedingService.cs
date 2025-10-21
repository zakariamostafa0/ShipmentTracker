using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShipmentTracker.Core.Entities;
using ShipmentTracker.Core.Enums;
using ShipmentTracker.Core.Interfaces;
using ShipmentTracker.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;

namespace ShipmentTracker.Infrastructure.Services;

public class DataSeedingService : IDataSeedingService
{
    private readonly ShipmentTrackerDbContext _context;
    private readonly ILogger<DataSeedingService> _logger;

    public DataSeedingService(ShipmentTrackerDbContext context, ILogger<DataSeedingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Checking if database needs seeding...");

            // Check if roles table is empty and seed roles
            var hasRoles = await _context.Roles.AnyAsync();
            if (!hasRoles)
            {
                await SeedRolesAsync();
            }

            // Check if users table is empty and seed admin user
            var hasUsers = await _context.Users.AnyAsync();
            if (!hasUsers)
            {
                await SeedAdminUserAsync();
            }

            _logger.LogInformation("Database seeding check completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during database seeding.");
            throw;
        }
    }

    private async Task SeedRolesAsync()
    {
        _logger.LogInformation("Seeding roles...");

        var roles = new[]
        {
            new Role { Name = "DataEntry", RoleType = RoleType.DataEntry, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Role { Name = "BranchAdmin", RoleType = RoleType.BranchAdmin, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Role { Name = "WarehouseOperator", RoleType = RoleType.WarehouseOperator, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Role { Name = "PortOperator", RoleType = RoleType.PortOperator, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Role { Name = "CarrierOperator", RoleType = RoleType.CarrierOperator, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Role { Name = "Client", RoleType = RoleType.Client, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Role { Name = "Admin", RoleType = RoleType.Admin, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        _context.Roles.AddRange(roles);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Roles seeded successfully.");
    }

    private async Task SeedAdminUserAsync()
    {
        _logger.LogInformation("Seeding admin user...");

        // Get Admin role
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleType == RoleType.Admin);
        if (adminRole == null)
        {
            _logger.LogError("Admin role not found. Please ensure roles are seeded first.");
            throw new InvalidOperationException("Admin role not found. Please ensure roles are seeded first.");
        }

        // Create admin user
        var adminUser = new User
        {
            UserName = "admin",
            Email = "admin@shipmenttracker.com",
            DisplayName = "System Administrator",
            PasswordHash = System.Text.Encoding.UTF8.GetBytes(BCrypt.Net.BCrypt.HashPassword("Admin123!")),
            IsActive = true,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync();

        // Assign Admin role to the user
        var userRole = new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin user created successfully. Username: admin, Password: Admin123!");
    }

    private static byte[] HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
    }
}
