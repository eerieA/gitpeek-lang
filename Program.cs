using gitpeek_lang.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddControllers();
builder.Services.AddHttpClient<GitHubCaller>();
builder.Services.AddTransient<GraphMaker>();
builder.Services.AddSingleton<LanguageColorService>();

builder.WebHost.ConfigureKestrel(options => {
    options.ListenAnyIP(80);
}
);

var app = builder.Build();
// Load the colors right after app being built
var colorService = app.Services.GetRequiredService<LanguageColorService>();
await colorService.InitAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// DEBUG
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/lib") || 
        context.Request.Path.StartsWithSegments("/css"))
    {
        var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", context.Request.Path.ToString().TrimStart('/'));
        Console.WriteLine($"Request URL: {context.Request.Path}");
        Console.WriteLine($"Physical path: {physicalPath}");
        Console.WriteLine($"File exists: {File.Exists(physicalPath)}");
        Console.WriteLine($"Directory exists: {Directory.Exists(Path.GetDirectoryName(physicalPath))}");
        Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
        // List contents of wwwroot
        Console.WriteLine("Contents of wwwroot:");
        foreach (var file in Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "*.*", SearchOption.AllDirectories))
        {
            Console.WriteLine(file);
        }
    }
    await next();
});
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream",
    OnPrepareResponse = ctx =>
    {
        Console.WriteLine($"Serving static file: {ctx.File.PhysicalPath}");
    }
});

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers(); // Enable API controllers

app.Run();