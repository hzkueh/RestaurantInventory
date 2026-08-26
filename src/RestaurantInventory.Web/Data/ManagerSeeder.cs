using Microsoft.AspNetCore.Identity;

namespace RestaurantInventory.Web.Data;

/// <summary>
/// Seeds the one Manager account the app signs in as. Idempotent: a Manager with the
/// configured email is created once and left untouched on every subsequent startup, so
/// clone-and-run produces a working login with no manual account setup.
/// </summary>
public static class ManagerSeeder
{
    /// <summary>
    /// Ensures a single Manager user exists for <paramref name="options"/>. Returns the
    /// existing or newly created user. Throws if creation fails (e.g. the configured
    /// password does not meet the Identity password policy) so a broken seed is loud.
    /// </summary>
    public static async Task<IdentityUser> SeedAsync(
        UserManager<IdentityUser> userManager,
        ManagerSeedOptions options)
    {
        var existing = await userManager.FindByEmailAsync(options.Email);
        if (existing is not null)
        {
            return existing;
        }

        var manager = new IdentityUser
        {
            UserName = options.Email,
            Email = options.Email,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(manager, options.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
            throw new InvalidOperationException($"Failed to seed the Manager account. {errors}");
        }

        return manager;
    }
}
