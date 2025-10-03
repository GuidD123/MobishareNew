using Mobishare.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Servizi Razor Pages
builder.Services.AddRazorPages();

// HttpClient per chiamare il Backend
builder.Services.AddHttpClient<IMobishareApiService, MobishareApiService>(client =>
{
    var baseUrl = builder.Configuration["MobishareApi:BaseUrl"] ?? "http://localhost:5000";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Sessione per mantenere login
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapRazorPages();

app.Run();