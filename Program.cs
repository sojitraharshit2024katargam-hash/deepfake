using DEEPFAKE.Helpers;
using DEEPFAKE.Services.EmailAnalysis;
using DEEPFAKE.Services.ImageGeneration;
using DEEPFAKE.Services.Implementations;
using DEEPFAKE.Services.Interfaces;
using DEEPFAKE.Services.UrlAnalysis;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<IEmailAnalysisService, EmailAnalysisService>();
builder.Services.AddScoped<EmailAnalysisRepository>();

builder.Services.AddScoped<IUrlAnalysisService, UrlAnalysisService>();
builder.Services.AddScoped<UrlAnalysisRepository>();

builder.Services.AddHttpClient<ImageGeneratorService>();
builder.Services.AddHttpClient();

var app = builder.Build();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
DatabaseInitializer.Initialize(connectionString);

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();   // important for MVC views
app.UseRouting();

app.UseSession();
app.UseAuthorization();

// API controllers
app.MapControllers();

// MVC routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();