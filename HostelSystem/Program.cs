using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using HostelSystem.Data;

var builder = WebApplication.CreateBuilder(args);

// Add Razor Pages
builder.Services.AddRazorPages();

// Register the database context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration
        .GetConnectionString("DefaultConnection")));

// Register Identity — this adds all the login/logout/register
// functionality and connects it to our AppDbContext
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    // Password requirements — keep them simple for now
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>();

// Where to redirect if someone tries to access a protected page
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});
var app = builder.Build();

// Seed the database with roles and a default admin account
// This runs once when the app starts
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    
    // Apply any pending migrations to create the tables in the new cloud database
    var context = services.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    
    await SeedData.InitialiseAsync(services);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();