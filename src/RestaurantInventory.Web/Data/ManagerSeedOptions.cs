namespace RestaurantInventory.Web.Data;

/// <summary>
/// Credentials for the single seeded Manager account, bound from the <c>SeedManager</c>
/// configuration section. These are demo credentials for a single-restaurant portfolio
/// app — not real secrets. The README (ticket 10) surfaces them as the sign-in details.
/// </summary>
public class ManagerSeedOptions
{
    public const string SectionName = "SeedManager";

    public string Email { get; set; } = "manager@restaurant.local";

    public string Password { get; set; } = "Manager!1";
}
