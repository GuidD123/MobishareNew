using Mobishare.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// SERVIZI APPLICATIVI
// ========================================

// Razor Pages
builder.Services.AddRazorPages();

builder.Services.AddDistributedMemoryCache(); 

// HttpClient per chiamate al Backend API
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

    //Durante lo sviluppo lascia questi due valori più permissivi:
    //ricodarsi di rimettere SecurePolicy.Always e SameSite.Strict.
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// HttpContextAccessor (necessario per _LoginPartial.cshtml)
builder.Services.AddHttpContextAccessor();

// ========================================
// SERVIZI INFRASTRUCTURE (IoT, Background Services)
// ========================================
builder.Services.AddSingleton<Mobishare.IoT.Gateway.Interfaces.IMqttGatewayEmulatorService, Mobishare.IoT.Gateway.Services.MqttGatewayEmulatorService>();

builder.Services.AddHostedService<Mobishare.WebApp.Services.MqttGatewayHostedService>();

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

// Authorization (se usi [Authorize] - opzionale senza Identity)
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