using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Mobishare.API.DTO;
using Mobishare.API.Middleware;
using Mobishare.Core.Data;
using Mobishare.Infrastructure.IoT.HostedServices;
using Mobishare.Infrastructure.IoT.Interfaces;
using Mobishare.Infrastructure.IoT.Services;
using Mobishare.Infrastructure.Services;
using Mobishare.Infrastructure.SignalRHubs;
using Mobishare.Infrastructure.SignalRHubs.HostedServices;
using Mobishare.Infrastructure.SignalRHubs.Services;
using System.Text;
using System.Text.Json;

/// <summary>
/// Punto di ingresso principale dell'applicazione ASP.NET Core per Mobishare.
/// Configura tutti i servizi necessari: database, autenticazione JWT, MQTT, CORS e Swagger.
/// </summary>

var builder = WebApplication.CreateBuilder(args);



#region Configurazione Controllers Base
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        //options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
#endregion


#region Configurazione Database
builder.Services.AddDbContext<MobishareDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion


/*
 Configurazione autenticazione JWT:
 - La chiave segreta (Jwt:Key) è definita in appsettings.json.
 - In fase di validazione il token viene controllato su:
    • Issuer (chi ha emesso il token)
    • Audience (chi può consumarlo)
    • Lifetime (scadenza)
    • Signature (firma con chiave simmetrica)
 - Per il progetto didattico la chiave è salvata nel file di configurazione.
   In un ambiente reale andrebbe spostata in variabili d'ambiente o secret manager.
*/

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
            }
        };
    });
#endregion


//Auth Gestore
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
// Registra i servizi MQTT
builder.Services.AddSingleton<IMqttIoTService, MqttIoTService>();
builder.Services.AddHostedService(sp => (MqttIoTService)sp.GetRequiredService<IMqttIoTService>());

// Registra il BackgroundService che integra IoT e DB e avvia automaticamente
builder.Services.AddHostedService<MqttIoTBackgroundService>();

//builder.Services.AddScoped<IIoTScenarioService, IoTScenarioService>();
#endregion


#region Configurazione Servizi Personalizzati
builder.Services.AddSingleton<PasswordService>();
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


//SignalR richiede AllowCredentials()per permettere la connessione WebSocket tra domini diversi 
#region Configurazione CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3001")
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
builder.Services.AddSignalR();
#endregion

builder.Services.AddSingleton<NotificationOutboxService>();

builder.Services.AddHostedService<NotificationRetryService>();



// Costruisce l'applicazione
var app = builder.Build();

// Seeder all'avvio (solo se vuoto)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<MobishareDbContext>();
    var passwordService = services.GetRequiredService<PasswordService>();

    // Applica migrazioni se ci sono
    context.Database.Migrate();

    // Popola dati di base se il DB è vuoto
    Mobishare.Infrastructure.Seed.DbSeeder.SeedDatabase(context, passwordService);
}

// Middleware custom -> ErrorHandling
app.UseExceptionHandling();


#region Configurazione Pipeline HTTP (Middleware)
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

// Middleware di sicurezza
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseHttpsRedirection();
app.UseCors();
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
            timestamp = DateTime.UtcNow,
            database = "Connected",
            version = "1.0.0"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Health Check Failed",
            detail: ex.Message,
            statusCode: 503
        );
    }
});
#endregion


//Registra tutti i controllers
app.MapControllers();

//Endpoint SignalR per notifiche in tempo reale -> hub raggiungibile a localhost
app.MapHub<NotificheHub>("/hub/notifiche");


#region Gestione Arresto Pulito
var cancellationTokenSource = new CancellationTokenSource();

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cancellationTokenSource.Cancel();
};

try
{
    await app.RunAsync(cancellationTokenSource.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Applicazione fermata correttamente");
}
catch (Exception ex)
{
    Console.WriteLine($"Errore durante l'avvio dell'applicazione: {ex.Message}");
    throw;
}
#endregion