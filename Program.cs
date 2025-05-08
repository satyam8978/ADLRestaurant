using ADLRestaurant.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<EmailHelper>();

builder.Services.AddRazorPages(options =>
{
    // Register session auth filter for all Razor Pages except Login/Register
    options.Conventions.AddFolderApplicationModelConvention("/", model =>
    {
        model.Filters.Add(new SessionAuthFilter());
    });
});

// Enable session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout to 1 day
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.UseSession(); // 👈 Important: must be before Razor Pages

app.UseAuthorization();

app.MapRazorPages();

app.Run();
