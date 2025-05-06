using ADLRestaurant.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton<EmailHelper>();

// Register the Session Auth Filter globally
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new SessionAuthFilter()); // Add your filter here
});

// Enable session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(1); // Set session timeout to 1 day
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

// Enable session middleware before authorization
app.UseSession(); // Enable session middleware

app.UseAuthorization();

app.MapRazorPages();

app.Run();
