using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using my_project.API.Extensions;
using my_project.API.Startup;
using my_project.API.Validation;
using my_project.Data;
using my_project.Service;
using NLog;
using NLog.Web;

// A logger that works before the host exists, so a crash during startup
// (e.g. a missing connection string) is also written to the log file
var startupLogger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // NLog replaces the default providers; the code keeps using ILogger<T>
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // The connection string comes from User Secrets (ConnectionStrings:Default), never from appsettings
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Connection string 'Default' is missing. Set ConnectionStrings:Default in User Secrets.");

    builder.Services.AddDataLayer(connectionString);
    builder.Services.AddServiceLayer();
    builder.Services.AddApiLayer(builder.Configuration, builder.Environment);

    // Enums travel as strings in JSON ("Coming", not 1).
    // An invalid request (400) is returned as ErrorResponse, like every other error
    builder.Services.AddControllers()
        .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
        .ConfigureApiBehaviorOptions(options =>
            options.InvalidModelStateResponseFactory = ValidationErrorResponseFactory.Create);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerWithBearer();
    builder.Services.AddClientCors(builder.Configuration);
    builder.Services.AddJwtAuthentication();

    // On a cloud host the app runs behind the host's proxy (HTTPS ends there). Hosting:BehindProxy = true
    // makes the app trust the X-Forwarded-Proto/For headers, so it knows the request was HTTPS
    var behindProxy = builder.Configuration.GetValue("Hosting:BehindProxy", false);
    if (behindProxy)
    {
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // The host's proxy address is not known in advance
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }

    var app = builder.Build();

    if (behindProxy)
    {
        app.UseForwardedHeaders();
    }

    // Database:MigrateOnStartup = true applies the migrations (schema + demo data) when the server starts,
    // so the system runs without a manual step in the database; then the first admin is created from SeedAdmin
    await app.InitializeDatabaseAsync();

    // First, so it catches exceptions from everything after it
    app.UseExceptionHandling();
    app.UseCorrelationId();
    app.UseRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else
    {
        // Browsers remember to use only HTTPS for this site
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    // Before authentication/authorization, so a browser's preflight (OPTIONS) request is answered
    app.UseClientCors();
    // Authentication (who is the user, from the token) must come before Authorization (what they may do)
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
// HostAbortedException is how the EF tools (Add-Migration) stop the host on purpose – not a crash
catch (Exception exception) when (exception is not HostAbortedException)
{
    startupLogger.Fatal(exception, "The server stopped because of an unhandled exception");
    throw;
}
finally
{
    // Writes any log lines still in memory before the process exits
    LogManager.Shutdown();
}

// Makes the Program class visible to the API tests (WebApplicationFactory<Program>)
public partial class Program
{
}
