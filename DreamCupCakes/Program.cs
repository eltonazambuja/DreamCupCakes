using DreamCupCakes.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using DreamCupCakes.Services;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ======================================================================
// 1) Connection string dinâmica (local/Azure/App Service/etc.)
// ======================================================================
string? connFromConfig = builder.Configuration.GetConnectionString("DefaultConnection");
string? home = Environment.GetEnvironmentVariable("HOME");
string connString;

if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")))
{
    // Prioriza variável de ambiente do App Service (Portal > Configuration)
    connString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
}
else if (!string.IsNullOrWhiteSpace(home))
{
    // Azure App Service (área gravável)
    var dataDir = Path.Combine(home!, "Data");
    Directory.CreateDirectory(dataDir);
    var dbPath = Path.Combine(dataDir, "dreamCupCakes.db");
    connString = $"Data Source={dbPath};Cache=Shared;";
}
else
{
    // Dev/local: se não houver conexão no appsettings, cria em pasta do projeto
    connString = !string.IsNullOrWhiteSpace(connFromConfig)
        ? connFromConfig!
        : $"Data Source={Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DreamCupCakes.db")}";

    var localDir = Path.GetDirectoryName(connString.Replace("Data Source=", "").Split(';')[0])!;
    if (!Directory.Exists(localDir)) Directory.CreateDirectory(localDir);
}

// ======================================================================
// 2) Serviços (DbContext, Auth, Session, etc.)
// ======================================================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connString)
);

// Serviço de Log
builder.Services.AddSingleton<IErrorLogger, ErrorLogger>();

// Autenticação por Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "DreamCupCakesCookie";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

// Cache e Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

// ======================================================================
// 2.1) Cultura padrão pt-BR (usa vírgula como separador decimal)
// ======================================================================
var defaultCulture = new CultureInfo("pt-BR");
defaultCulture.NumberFormat.NumberDecimalSeparator = ",";
defaultCulture.NumberFormat.CurrencyDecimalSeparator = ",";

// aplica cultura padrão pra todas as threads
CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

// registra as culturas suportadas no pipeline de localização
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { defaultCulture };

    options.DefaultRequestCulture = new RequestCulture(defaultCulture);
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// ======================================================================

var app = builder.Build();

// ======================================================================
// 3) Migrações automáticas (cria/atualiza o .db no Azure)
// ======================================================================
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Erro ao criar/migrar o banco no Azure.");
}
// ======================================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ======================================================================
// 4) RequestLocalization -> garante pt-BR na formatação Razor
// ======================================================================
var locOptions = app.Services
    .GetRequiredService<IOptions<RequestLocalizationOptions>>()
    .Value;
app.UseRequestLocalization(locOptions);

// ======================================================================

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Ordem recomendada: Auth/Authorization, depois Session, depois endpoints
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
