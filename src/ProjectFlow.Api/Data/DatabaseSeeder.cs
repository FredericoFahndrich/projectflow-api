using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectFlow.Api.Domain;

namespace ProjectFlow.Api.Data;

public static class DatabaseSeeder
{
    public static async Task SeedBootstrapAdminAsync(IServiceProvider services, IConfiguration configuration, CancellationToken cancellationToken)
    {
        var email = configuration["BootstrapAdmin:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var db = services.GetRequiredService<AppDbContext>();
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            return;
        }

        var user = new User
        {
            Name = "ProjectFlow Admin",
            Email = email,
            PasswordHash = string.Empty,
            Role = UserRole.Admin
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
    }
}

