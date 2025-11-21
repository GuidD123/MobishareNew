using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Mobishare.Core.Data;
using Mobishare.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// SERVIZI APPLICATIVI
// ========================================

#region Configurazione Database
builder.Services.AddDbContext<MobishareDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion

// Razor Pages
builder.Services.AddRazorPages();

// SignalR
builder.Services.AddSignalR();

builder.Services.AddDistributedMemoryCache();

// configura l'HttpClient per chiamate al Backend API
builder.Services.AddHttpClient<IMobishareApiService, MobishareApiService>(client =>
{
    var baseUrl = builder.Configuration["MobishareApi:BaseUrl"] ?? "https://localhost:7001";
    if (baseUrl.EndsWith("/"))
        baseUrl = baseUrl.TrimEnd('/');
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

// Alternativa: HttpClient generico (se non hai IMobishareApiService)
builder.Services.AddHttpClient();

// Session per gestire login/logout
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = ".Mobishare.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

    //Durante lo sviluppo lascia questi due valori pi� permissivi:
    //ricodarsi di rimettere SecurePolicy.Always e SameSite.Strict.
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// HttpContextAccessor (necessario per _LoginPartial.cshtml)
builder.Services.AddHttpContextAccessor();

// ========================================
// AUTENTICAZIONE E AUTORIZZAZIONE
// ========================================

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

// ========================================
// NOTA: Gateway IoT rimossi da WebApp
// ========================================
// I gateway MQTT sono ora gestiti dal progetto standalone Mobishare.IoT.Gateway.
// WebApp si occupa SOLO del frontend (Razor Pages) e chiamate API.
// Per avviare i gateway, eseguire separatamente Mobishare.IoT.Gateway.exe

// Cookie Policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false; // Per sviluppo
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
});

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// CORS (se necessario per chiamate API da JavaScript)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBackend", policy =>
    {
        policy.WithOrigins(builder.Configuration["MobishareApi:BaseUrl"] ?? "https://localhost:7001")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ========================================
// BUILD APP
// ========================================
var app = builder.Build();

// ========================================
// MIDDLEWARE PIPELINE
// ========================================

// Exception Handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage(); // Mostra errori dettagliati in sviluppo
}

// Security Headers 
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
    context.Response.Headers.Append("Pragma", "no-cache");
    context.Response.Headers.Append("Expires", "0");
    await next();
});

// HTTPS Redirect
app.UseHttpsRedirection();

// File Statici (CSS, JS, immagini)
app.UseStaticFiles();

// Cookie Policy
app.UseCookiePolicy();

// Routing
app.UseRouting(); //definisce endpoint corrente 

// Session -> dev'essere disponibile in tutte le pagine razor ma prima di MapRazoPages()
app.UseSession();

// CORS (se configurato)
app.UseCors("AllowBackend");

app.UseAuthentication();

app.UseAuthorization();

// Endpoint manuale per logout rapido
app.MapGet("/Account/Logout", async context =>
{
    // Svuota sessione
    context.Session.Clear();

    context.Response.Cookies.Delete(".Mobishare.Session");
    context.Response.Cookies.Delete(".AspNetCore.Mvc.CookieTempDataProvider");
    context.Response.Cookies.Delete("RememberMe");

    context.Response.Redirect("/Index");
    await Task.CompletedTask;
});
 
// Map Razor Pages
app.MapRazorPages();

// ========================================
// RUN APP
// ========================================
app.Run();