using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using my_project.API.Auth;
using my_project.API.BackgroundJobs;
using my_project.API.Middleware;
using my_project.Core.Interfaces.Infrastructure;
using my_project.Data.Storage;
using my_project.Service.Notifications;
using my_project.Service.Options;

namespace my_project.API.Extensions;

public static class ServiceCollectionExtensions
{
    public const string ClientCorsPolicy = "Client";

    private const int MaxTokenLifetimeMinutes = 24 * 60;
    private const int MaxEmailTimeoutSeconds = 300;
    private const int MaxReminderIntervalMinutes = 24 * 60;

    // Tolerance for a small clock difference between servers. Kept short: an expired token
    // is accepted for at most this long (the default is 5 minutes)
    private static readonly TimeSpan TokenClockSkew = TimeSpan.FromSeconds(30);

    // What the Service layer needs but does not register itself: Options, ICurrentUser, IEmailSender, IFileStorage
    public static IServiceCollection AddApiLayer(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Validated when the server starts: a missing or weak key stops the server right away,
        // instead of failing on the first login. The messages never contain the key itself
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .Validate(jwt => !string.IsNullOrWhiteSpace(jwt.Issuer), "Jwt:Issuer is missing.")
            .Validate(jwt => !string.IsNullOrWhiteSpace(jwt.Audience), "Jwt:Audience is missing.")
            .Validate(
                jwt => Encoding.UTF8.GetByteCount(jwt.Key) >= JwtSettings.MinKeyBytes,
                $"Jwt:Key is missing or shorter than {JwtSettings.MinKeyBytes} bytes. " +
                "Development: set it in User Secrets. Production: set Jwt__Key in the hosting environment's secret settings.")
            .Validate(jwt => jwt.ExpiryMinutes is > 0 and <= MaxTokenLifetimeMinutes,
                $"Jwt:ExpiryMinutes must be between 1 and {MaxTokenLifetimeMinutes}.")
            .ValidateOnStart();

        // Checked only when sending is enabled. The messages never contain the password
        services.AddOptions<EmailSettings>()
            .Bind(configuration.GetSection(EmailSettings.SectionName))
            .Validate(email => !email.Enabled || !string.IsNullOrWhiteSpace(email.Host),
                "Email:Host is missing (or set Email:Enabled to false).")
            .Validate(email => !email.Enabled || email.Port is > 0 and <= 65535,
                "Email:Port must be between 1 and 65535.")
            .Validate(email => !email.Enabled || MailAddress.TryCreate(email.From, out _),
                "Email:From must be a valid email address.")
            .Validate(email => !email.Enabled || string.IsNullOrEmpty(email.Username) == string.IsNullOrEmpty(email.Password),
                "Email:Username and Email:Password must be set together. " +
                "Development: set Email:Password in User Secrets. Production: set Email__Password in the hosting environment's secret settings.")
            .Validate(email => email.TimeoutSeconds is > 0 and <= MaxEmailTimeoutSeconds,
                $"Email:TimeoutSeconds must be between 1 and {MaxEmailTimeoutSeconds}.")
            .ValidateOnStart();

        services.AddOptions<ReminderSettings>()
            .Bind(configuration.GetSection(ReminderSettings.SectionName))
            .Validate(reminders => reminders.IntervalMinutes is > 0 and <= MaxReminderIntervalMinutes,
                $"Reminders:IntervalMinutes must be greater than 0 and at most {MaxReminderIntervalMinutes}.")
            .ValidateOnStart();

        services.Configure<DisplaySettings>(configuration.GetSection(DisplaySettings.SectionName));
        // The rules are in UploadSettingsValidator (Service layer, registered by AddServiceLayer)
        services.AddOptions<UploadSettings>()
            .Bind(configuration.GetSection(UploadSettings.SectionName))
            .ValidateOnStart();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        // Email:Enabled (validated above) chooses the sender: real SMTP, or only a log line.
        // So a machine without an SMTP server, or an automated test, never sends a real email by mistake
        services.AddSingleton<IEmailSender>(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<EmailSettings>>().Value.Enabled
                ? ActivatorUtilities.CreateInstance<SmtpEmailSender>(serviceProvider)
                : ActivatorUtilities.CreateInstance<LoggingEmailSender>(serviceProvider));

        var uploadsPath = Path.Combine(environment.ContentRootPath, "uploads");
        services.AddSingleton<IFileStorage>(new LocalFileStorage(uploadsPath));

        // Not started by the EF tools (Add-Migration builds the host but never starts it)
        services.AddHostedService<ReminderBackgroundService>();

        return services;
    }

    // Every request with "Authorization: Bearer <token>" is checked against the same JwtSettings
    // that JwtTokenService signs with: signature, algorithm, issuer, audience and lifetime.
    // A valid token fills HttpContext.User, which CurrentUser and [Authorize(Roles = ...)] read
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, jwtSettings) =>
            {
                var jwt = jwtSettings.Value;

                // The token stores the short names ("nameid", "email", "role"); they are mapped back to
                // ClaimTypes.NameIdentifier / Email / Role – the names JwtTokenService writes and CurrentUser reads
                options.MapInboundClaims = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    RequireSignedTokens = true,
                    // Only the algorithm we sign with – rejects "none" and algorithm-switching tricks
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],

                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,

                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TokenClockSkew,

                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = AuthErrorResponses.WriteUnauthorizedAsync,
                    OnForbidden = AuthErrorResponses.WriteForbiddenAsync
                };
            });

        return services;
    }

    // Only the client addresses listed in Cors:AllowedOrigins may call the API from a browser.
    // No credentials (cookies) – the JWT travels in the Authorization header
    public static IServiceCollection AddClientCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(ClientCorsPolicy, policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            // Lets the React client read the id that appears in error messages
            .WithExposedHeaders(CorrelationIdMiddleware.HeaderName)));

        return services;
    }

    // Swagger with an "Authorize" button that sends "Bearer <token>"
    public static IServiceCollection AddSwaggerWithBearer(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            var bearerScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Paste the token from /api/auth/login (without the word Bearer)",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            };

            options.AddSecurityDefinition("Bearer", bearerScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement { [bearerScheme] = [] });
        });

        return services;
    }
}
