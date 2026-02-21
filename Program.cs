using DEEPFAKE.Helpers;
using DEEPFAKE.Services.EmailAnalysis;
using DEEPFAKE.Services.ImageGeneration;
using DEEPFAKE.Services.Implementations;
using DEEPFAKE.Services.Interfaces;
using DEEPFAKE.Services.UrlAnalysis;

var builder = WebApplication.CreateBuilder(args);

// ===================================
// Add services
// ===================================
builder.Services.AddControllersWithViews();

// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Email Services
builder.Services.AddScoped<IEmailAnalysisService, EmailAnalysisService>();
builder.Services.AddScoped<EmailAnalysisRepository>();

// URL Services
builder.Services.AddScoped<IUrlAnalysisService, UrlAnalysisService>();
builder.Services.AddScoped<UrlAnalysisRepository>();

builder.Services.AddHttpClient<ImageGeneratorService>();

builder.Services.AddHttpClient();

var app = builder.Build();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
DatabaseInitializer.Initialize(connectionString);


// ===================================
// Pipeline
// ===================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();

app.UseAuthorization();


// ===================================
// 🔥 IMPORTANT: ENABLE API CONTROLLERS
// ===================================
app.MapControllers();   // <<< THIS FIXES 405 ERROR


// ===================================
// MVC Routes
// ===================================
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
