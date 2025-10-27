using DreamCupCakes.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using DreamCupCakes.Services;

var builder = WebApplication.CreateBuilder(args);

// ======================================================================
// 1) CONNECTION STRING: usa env var se existir; sen�o, monta caminho seguro
//    No Azure, HOME existe e aponta para /home (Linux) ou D:\home (Windows).
// ======================================================================
string? connFromConfig = builder.Configuration.GetConnectionString("DefaultConnection");
string? home = Environment.GetEnvironmentVariable("HOME");
string connString;

if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")))
{
    // Prioriza vari�vel de ambiente do App Service (Portal > Configuration)
    connString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
}
else if (!string.IsNullOrWhiteSpace(home))
{
    // Azure App Service (�rea grav�vel)
    var dataDir = Path.Combine(home!, "Data");
    Directory.CreateDirectory(dataDir);
    var dbPath = Path.Combine(dataDir, "dreamCupCakes.db");
    connString = $"Data Source={dbPath};Cache=Shared;";
}
else
{
    // Dev/local: se n�o houver conex�o no appsettings, cria em pasta do projeto
    connString = !string.IsNullOrWhiteSpace(connFromConfig)
        ? connFromConfig!
        : $"Data Source={Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DreamCupCakes.db")}";
    var localDir = Path.GetDirectoryName(connString.Replace("Data Source=", "").Split(';')[0])!;
    if (!Directory.Exists(localDir)) Directory.CreateDirectory(localDir);
}

// ======================================================================
// 2) Servi�os (DbContext, Auth, Session, etc.)
// ======================================================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connString)
);

// Servi�o de Log
builder.Services.AddSingleton<IErrorLogger, ErrorLogger>();

// Autentica��o por Cookie
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

var app = builder.Build();

// ======================================================================
/* 3) Migra��es autom�ticas (cria/atualiza o .db no Azure) */
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // Se estiver usando SQLitePCLRaw, a linha abaixo geralmente n�o � necess�ria,
    // mas se ver erro de inicializa��o nativa, pode habilitar:
    // SQLitePCL.Batteries_V2.Init();

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
