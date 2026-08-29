using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantInventory.Core.Persistence;
using RestaurantInventory.Core.Services;
using RestaurantInventory.Core.Services.Insight;
using RestaurantInventory.Web.Components;
using RestaurantInventory.Web.Data;
using RestaurantInventory.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Layer the git-ignored .env (AI key, see ADR-0002 / .env.example) into configuration before
// anything reads it. Missing file is a no-op — the AI page then reports itself unavailable.
DotEnv.Load(builder.Configuration, builder.Environment.ContentRootPath);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Inventory")));

// The ledger seam every Blazor page calls into (ticket 02).
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<InventoryService>();

// --- AI summary (ticket 08 / ADR-0002): the insight seam and its Gemini implementation. ---
// The provider is hidden behind IInventoryInsightService; the summary page depends only on the
// interface, so swapping providers is a single new implementation. GeminiInsightService uses a
// typed HttpClient via IHttpClientFactory (no third-party AI SDK). With no key configured it
// degrades to an "unavailable" result, keeping AI off the critical path of core operations.
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));
builder.Services.AddHttpClient<IInventoryInsightService, GeminiInsightService>(client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// --- Auth (ticket 03): cookie-based ASP.NET Core Identity, one seeded Manager. ---
// Identity lives in its own DbContext (and its own migrations-history table) inside the
// same SQLite file, so the Core domain project stays free of any auth dependency.
builder.Services.AddDbContext<AppIdentityDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("Inventory"),
        sqlite => sqlite.MigrationsHistoryTable(AppIdentityDbContext.MigrationsHistoryTableName)));

builder.Services.Configure<ManagerSeedOptions>(
    builder.Configuration.GetSection(ManagerSeedOptions.SectionName));

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
    })
    .AddIdentityCookies();

// Any server-side auth challenge lands on the login page (the Blazor guard also handles
// interactive navigation via AuthorizeRouteView + RedirectToLogin).
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
});

builder.Services.AddIdentityCore<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppIdentityDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization();

var app = builder.Build();

// Apply migrations on startup so a fresh clone-and-run produces the schema for both the
// inventory ledger and the Identity tables with no manual step, then seed the Manager.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    services.GetRequiredService<InventoryDbContext>().Database.Migrate();
    services.GetRequiredService<AppIdentityDbContext>().Database.Migrate();

    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var seedOptions = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ManagerSeedOptions>>().Value;
    await ManagerSeeder.SeedAsync(userManager, seedOptions);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Sign-out endpoint the nav's Sign out form posts to (ticket 03).
app.MapPost("/account/logout", async (SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
});

app.Run();

// Exposed so the WebApplicationFactory in AuthGuardTests can boot this app for end-to-end
// auth/guard tests against the real request pipeline.
public partial class Program;
