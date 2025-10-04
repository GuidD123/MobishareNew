using Mobishare.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// SERVIZI
// ========================================

builder.Services.AddRazorPages();

builder.Services.AddHttpClient<IMobishareApiService, MobishareApiService>(client =>
{
    var baseUrl = builder.Configuration["MobishareApi:BaseUrl"] ?? "http://localhost:5000";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ========================================
// BUILD APP
// ========================================

var app = builder.Build();

// ========================================
// MIDDLEWARE PIPELINE
// ========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Security Headers - USA APPEND invece di Add
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapRazorPages();

app.Run();