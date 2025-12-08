using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

// Try to register ZoomService, but don't fail if configuration is missing
try
{
    var zoomConfig = builder.Configuration.GetSection("Zoom");
    if (!string.IsNullOrEmpty(zoomConfig["AccountId"]))
    {
        builder.Services.AddHttpClient<ZoomService>();
        builder.Services.AddScoped<ZoomService>();
        Console.WriteLine("DEBUG: ZoomService registered successfully");
    }
    else
    {
        Console.WriteLine("DEBUG: Zoom configuration missing, ZoomService not registered");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"DEBUG: Failed to register ZoomService: {ex.Message}");
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();