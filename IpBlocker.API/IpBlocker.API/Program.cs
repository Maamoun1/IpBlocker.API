using IpBlocker.API.BackgroundServices;
using IpBlocker.API.Common;
using IpBlocker.API.ExternalServices;
using IpBlocker.API.Repositories;
using IpBlocker.API.Services;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();


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


});

// ═══════════════════════════════════════════════════════════════════════
// 3. TYPED CONFIGURATION (Options Pattern)
// Binds appsettings.json "GeoLocationApi" section to GeoLocationApiSettings.
// Injected as IOptions<GeoLocationApiSettings> wherever needed.
// ═══════════════════════════════════════════════════════════════════════
builder.Services.Configure<GeoLocationApiSettings>(
    builder.Configuration.GetSection(GeoLocationApiSettings.SectionName));

// ═══════════════════════════════════════════════════════════════════════
// 4. HTTP CLIENT (IHttpClientFactory pattern)
// NEVER use `new HttpClient()` — causes socket exhaustion under load.
// AddHttpClient<T>() registers a typed client with connection pooling.
// The factory manages HttpMessageHandler lifetime (not the HttpClient itself).
// ═══════════════════════════════════════════════════════════════════════
builder.Services.AddHttpClient<IGeoLocationApiClient, GeoLocationApiClient>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5)); // Recommended: rotate handlers every 5 min

// ═══════════════════════════════════════════════════════════════════════
// 5. REPOSITORIES — SINGLETON lifetime
// WHY SINGLETON? These hold the in-memory ConcurrentDictionary/Queue.
// If Scoped: each request gets a fresh empty collection → data lost!
// If Transient: same problem, worse.
// Singleton = one instance for the entire application lifetime. Correct.
// ═══════════════════════════════════════════════════════════════════════
builder.Services.AddSingleton<IBlockedCountryRepository, BlockedCountryRepository>();
builder.Services.AddSingleton<ITemporalBlockRepository, TemporalBlockRepository>();
builder.Services.AddSingleton<ILogRepository, LogRepository>();

// ═══════════════════════════════════════════════════════════════════════
// 6. SERVICES — SCOPED lifetime
// WHY SCOPED? Services have no state of their own — they're orchestrators.
// A new instance per request is clean and avoids any request bleed-over.
// They depend on Singleton repositories — that's fine (longer lifetime > shorter).
// NOTE: Never inject Scoped into Singleton — that's "Captive Dependency" anti-pattern.
// ═══════════════════════════════════════════════════════════════════════
builder.Services.AddScoped<ICountryService, CountryService>();
builder.Services.AddScoped<IIpService, IpService>();
builder.Services.AddScoped<ILogService, LogService>();

// ═══════════════════════════════════════════════════════════════════════
// 7. BACKGROUND SERVICE
// AddHostedService registers it as a Singleton-lifetime hosted service.
// The framework starts ExecuteAsync when the app starts,
// and cancels the CancellationToken when the app shuts down.
// ═══════════════════════════════════════════════════════════════════════
builder.Services.AddHostedService<TemporalBlockCleanupService>();



builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();


var app = builder.Build();



app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "IP Blocker API v1");
    options.RoutePrefix = string.Empty; // Swagger at root: http://localhost:5000/
});

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

