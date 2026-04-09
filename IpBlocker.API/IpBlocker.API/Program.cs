using IpBlocker.API.BackgroundServices;
using IpBlocker.API.Common;
using IpBlocker.API.ExternalServices;
using IpBlocker.API.Repositories;
using IpBlocker.API.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── Swagger ────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IP Blocker API",
        Version = "v1",
        Description = "Manages blocked countries and validates IP addresses " +
                      "using ipapi.co geolocation. No database — fully in-memory.",
        Contact = new OpenApiContact
        {
            Name = "Backend Assignment",
            Email = "dev@atechnologies.info"
        }
    });
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
});

// ── Typed configuration ────────────────────────────────────────────────────
builder.Services.Configure<GeoLocationApiSettings>(
    builder.Configuration.GetSection(GeoLocationApiSettings.SectionName));

// ── HttpClient registration ────────────────────────────────────────────────
//

builder.Services.AddHttpClient<IGeoLocationApiClient, GeoLocationApiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "IpBlockerAPI/1.0");
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5));

// ── Repositories — Singleton (hold in-memory state across all requests) ────
builder.Services.AddSingleton<IBlockedCountryRepository, BlockedCountryRepository>();
builder.Services.AddSingleton<ITemporalBlockRepository, TemporalBlockRepository>();
builder.Services.AddSingleton<ILogRepository, LogRepository>();

// ── Services — Scoped (stateless, new instance per request) ───────────────
builder.Services.AddScoped<ICountryService, CountryService>();
builder.Services.AddScoped<IIpService, IpService>();
builder.Services.AddScoped<ILogService, LogService>();

// ── Background service ─────────────────────────────────────────────────────
builder.Services.AddHostedService<TemporalBlockCleanupService>();

// ── Logging ────────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ── Build & configure middleware pipeline ──────────────────────────────────
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "IP Blocker API v1");
    options.RoutePrefix = string.Empty; // Swagger at root: https://localhost:7244/
});

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
