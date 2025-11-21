using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Mobishare.API.BackgroundServices;
using Mobishare.API.Middleware;
using Mobishare.Core.Data;
using Mobishare.Infrastructure.IoT.HostedServices;
using Mobishare.Infrastructure.IoT.Interfaces;
using Mobishare.Infrastructure.IoT.Services;
using Mobishare.Infrastructure.PhilipsHue;
using Mobishare.Infrastructure.Services;
using Mobishare.Infrastructure.SignalRHubs;
using System.Text;
using System.Text.Json;

/// <summary>
/// Punto di ingresso principale dell'applicazione ASP.NET Core per Mobishare.
/// Configura tutti i servizi necessari: database, autenticazione JWT, MQTT, CORS, SignalR e Swagger.
/// </summary>

var builder = WebApplication.CreateBuilder(args);

#region Configurazione Controllers Base
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});
#endregion


#region Configurazione Database
builder.Services.AddDbContext<MobishareDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion


#region Configurazione Autenticazione JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key non configurata");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("Autenticazione fallita: " + context.Exception.Message);
                return Task.CompletedTask;
            },

            OnTokenValidated = context =>
            {
                Console.WriteLine("Token validato per: " + context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }, 

            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub/notifiche"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });
#endregion


#region Configurazione Autorizzazione (Policy)
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CanViewOwnData", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim != null;
        });
    })
    .AddPolicy("AdminOnly", policy =>
    {
        policy.RequireRole("gestore");
    });
#endregion


#region Configurazione MQTT e IoT
builder.Services.AddSingleton<IMqttIoTService, MqttIoTService>();
builder.Services.AddHostedService(sp => (MqttIoTService)sp.GetRequiredService<IMqttIoTService>());
builder.Services.AddHostedService<MqttIoTBackgroundService>();
// Nota: MqttGatewayManager è gestito dalla WebApp, non dall'API
#endregion


#region Configurazione Servizi Personalizzati
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddScoped<IRideMonitoringService, RideMonitoringService>();
builder.Services.AddHostedService<RideMonitoringBackgroundService>();

builder.Services.AddHttpClient("PhilipsHue", client =>
{
    var hueUrl = builder.Configuration["Hue:BaseUrl"];

    if (!string.IsNullOrEmpty(hueUrl))
    {
        client.BaseAddress = new Uri(hueUrl);
        Console.WriteLine($"Philips Hue configurato: {hueUrl}");
    }
    else
    {
        // Philips Hue opzionale - costruisci URL da host e porta
        var hueHost = builder.Configuration["Hue:Host"] ?? "localhost";
        var huePort = builder.Configuration["Hue:Port"] ?? "8000";
        var hueUsername = builder.Configuration["Hue:Username"] ?? "newdeveloper";
        
        var fallbackUrl = $"http://{hueHost}:{huePort}/api/{hueUsername}/";
        client.BaseAddress = new Uri(fallbackUrl);
        
        Console.WriteLine($"Philips Hue: BaseUrl non trovato, uso fallback: {fallbackUrl}");
    }
    
    // Timeout per evitare attese infinite
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddScoped<PhilipsHueControl>();

// Servizio di sincronizzazione iniziale Philips Hue (opzionale)
// Sincronizza lo stato delle luci con il DB all'avvio
builder.Services.AddHostedService<PhilipsHueSyncService>();
#endregion


#region PagamentoService
builder.Services.AddScoped<PagamentoService>();
#endregion


#region Configurazione Swagger (Documentazione API)
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Mobishare API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Inserisci 'Bearer' seguito da spazio e token JWT",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
#endregion


#region Configurazione CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? new[] { "https://localhost:7268" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
#endregion


#region Configurazione Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
#endregion


#region Configurazione SignalR
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(120);
});
#endregion

var app = builder.Build();

#region Seeder
// Seeder iniziale
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<MobishareDbContext>();
    var passwordService = services.GetRequiredService<PasswordService>();
    context.Database.Migrate();
    Mobishare.Infrastructure.Seed.DbSeeder.SeedDatabase(context, passwordService);
}
#endregion

#region ExceptionHandling
// Middleware custom
app.UseExceptionHandling();
#endregion 

#region Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Sicurezza header
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseHttpsRedirection();

// CORS deve venire PRIMA di Authentication e SignalR
app.UseCors("AllowWebApp");

app.UseAuthentication();
app.UseAuthorization();
#endregion

#region Endpoint Health Check
app.MapGet("/health", async (MobishareDbContext context) =>
{
    try
    {
        await context.Database.CanConnectAsync();
        return Results.Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.Now,
            database = "Connected",
            version = "1.0.0"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(title: "Health Check Failed", detail: ex.Message, statusCode: 503);
    }
});
#endregion

#region CONTROLLERS + HUB NOTIFICHE
// Controllers + Hub
app.MapControllers();
app.MapHub<NotificheHub>("/hub/notifiche");
#endregion

#region Gestione Arresto Pulito
var cts = new CancellationTokenSource();

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    await app.RunAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Applicazione fermata correttamente");
}
catch (Exception ex)
{
    Console.WriteLine($"Errore durante l'avvio: {ex.Message}");
    throw;
}
#endregion
