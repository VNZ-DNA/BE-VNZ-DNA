using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;

namespace VNZ.Api.Data;

public static class DevelopmentDataSeeder
{
    public const string AdminEmail = "yinexa2180@blobapps.com";
    public const string AdminPassword = "Yinexa@2180";

    public static async Task SeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var role = await dbContext.Roles.SingleOrDefaultAsync(x => x.Type == "Admin");
        if (role is null)
        {
            role = new Role
            {
                Id = Guid.NewGuid(),
                Type = "Admin",
                CreatedAt = DateTimeOffset.UtcNow
            };

            dbContext.Roles.Add(role);
            await dbContext.SaveChangesAsync();
        }

        var admin = await dbContext.Users
            .SingleOrDefaultAsync(x => x.Email.ToLower() == AdminEmail);

        if (admin is null)
        {
            admin = new User
            {
                Id = Guid.NewGuid(),
                FullName = "Yinexa Test Admin",
                Email = AdminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword),
                RoleId = role.Id,
                IsActive = true,
                EmploymentStatus = EmploymentStatus.Working,
                IsPublished = false,
                CreateAt = DateTimeOffset.UtcNow,
                ResetPasswordCode = 0
            };

            dbContext.Users.Add(admin);
        }
        else
        {
            admin.RoleId = role.Id;
            admin.IsActive = true;
        }

        await dbContext.SaveChangesAsync();
    }
}
