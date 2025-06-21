using System;
using System.Linq;
using System.Threading.Tasks;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Domain.Entities;
using FoodOrderingSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FoodOrderingSystem.Persistence
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
            var context = services.GetRequiredService<ApplicationDbContext>();

            try
            {
                logger.LogInformation("Applying migrations...");
                await context.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully");

                await SeedDataAsync(context, services, logger);
                logger.LogInformation("Seed completed successfully");

                // Reset admin user password to fix any hashing issues
                await ResetAdminPasswordAsync(context, services, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during database initialization");
                throw;
            }
        }

        private static async Task SeedDataAsync(ApplicationDbContext context, IServiceProvider services, ILogger logger)
        {
            // Only seed if no admin users exist
            if (!await context.Users.AnyAsync(u => u.Role == UserRole.Admin))
            {
                logger.LogInformation("Seeding admin user...");

                var authService = services.GetRequiredService<IAuthService>();

                // Create admin user with hashed password
                string passwordHash = authService.CreatePasswordHash("P@ssw0rd123");

                var adminUser = new User
                {
                    Name = "HazemAdmin",
                    PhoneNumber = "01006864532",
                    Email = "Hazem.army15@gmail.com",
                    PasswordHash = passwordHash,
                    Role = UserRole.Admin,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    LastModifiedBy = "System"
                };

                await context.Users.AddAsync(adminUser);
                await context.SaveChangesAsync();
                logger.LogInformation("Admin user created successfully");
            }
            else
            {
                logger.LogInformation("Admin user already exists");
            }
        }

        private static async Task ResetAdminPasswordAsync(ApplicationDbContext context, IServiceProvider services, ILogger logger)
        {
            try
            {
                logger.LogInformation("Checking admin user password...");

                // Get the admin user
                var adminUser = await context.Users
                    .FirstOrDefaultAsync(u => u.Email == "Hazem.army15@gmail.com" && u.Role == UserRole.Admin);

                if (adminUser != null)
                {
                    var authService = services.GetRequiredService<IAuthService>();

                    // Update password using the fixed hashing method
                    adminUser.PasswordHash = authService.CreatePasswordHash("P@ssw0rd123");
                    adminUser.LastModifiedAt = DateTime.UtcNow;

                    context.Users.Update(adminUser);
                    await context.SaveChangesAsync();

                    logger.LogInformation("Admin password has been reset successfully");
                }
                else
                {
                    logger.LogWarning("Admin user not found for password reset");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error resetting admin password");
            }
        }
    }
}