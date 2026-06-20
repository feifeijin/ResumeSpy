using System;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ResumeSpy.Infrastructure.Data;
using ResumeSpy.UI.Authorization;
using ResumeSpy.UI.Extensions;
using ResumeSpy.UI.HealthChecks;
using ResumeSpy.UI.Middlewares;
using ResumeSpy.Infrastructure.Configuration;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.UI.Swagger;

// Enable non-UTF-8 encodings (GBK, Shift-JIS, EUC-JP, EUC-KR, …) so the
// resume import service can read legacy CJK text files without byte loss.
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// Sentry error tracking. DSN is read from the SENTRY_DSN env var (preferred in
// hosted environments) with a fallback to the "Sentry:Dsn" config key. When
// the DSN is empty the SDK initialises in a no-op mode, so local development
// runs without sending events.
builder.WebHost.UseSentry(options =>
{
    options.Dsn = Environment.GetEnvironmentVariable("SENTRY_DSN")
                  ?? builder.Configuration["Sentry:Dsn"]
                  ?? string.Empty;
    options.Environment = builder.Environment.EnvironmentName;
    options.TracesSampleRate = builder.Configuration.GetValue<double?>("Sentry:TracesSampleRate") ?? 0.1;
    options.SendDefaultPii = false;
});

var envConnection = Environment.GetEnvironmentVariable("DB_CONNECTION");
var connectionString = !string.IsNullOrEmpty(envConnection)
    ? envConnection
    : builder.Configuration.GetConnectionString("PrimaryDbConnection");
if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException(
        "DB_CONNECTION environment variable or ConnectionStrings:PrimaryDbConnection is not configured. " +
        "Set it in .env.local, .env.dev, or appsettings.Development.json.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Auth is Supabase JWT; the local ApplicationUser store exists only so
// IdentityLinkingService can look up / create users by email. AddIdentityCore
// gives us UserManager without AddIdentity's cookie auth scheme, SignInManager,
// password-policy surface, or token providers — none of which have a reachable
// caller and which would otherwise advertise a vestigial password sign-in path.
builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Enable annotations
    options.EnableAnnotations();
    options.DocumentFilter<FormDataDocumentFilter>();
    
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ResumeSpy API",
        Version = "v1",
        Description = "ResumeSpy Web API documentation"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT bearer token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            securityScheme,
            Array.Empty<string>()
        }
    });
});
builder.Services.AddHttpClient();

builder.Services.AddControllersWithViews();
builder.Services.RegisterService();

// Add caching services
builder.Services.AddMemoryCache();

// Short-lived HTTP client used exclusively by SupabaseAuthHealthCheck.
// Kept separate from the regular IHttpClientFactory clients so a slow
// Supabase OIDC response does not interfere with production traffic.
builder.Services.AddHttpClient("SupabaseHealth", client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
});

// Health checks:
//   /health     – liveness (no dependencies)
//   /health/db  – readiness: DB connectivity
//   /health/auth – readiness: Supabase OIDC connectivity (JWT validation prerequisite)
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "ready" })
    .AddCheck<SupabaseAuthHealthCheck>(
        name: "supabase-auth",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "auth", "ready" });

// Register ILogger service
builder.Services.AddLogging();


// Load translator settings from configuration
builder.Services.Configure<TranslatorSettings>(builder.Configuration.GetSection("TranslatorSettings"));
builder.Services.Configure<SupabaseSettings>(builder.Configuration.GetSection("Supabase"));
builder.Services.Configure<AnonymousUserSettings>(builder.Configuration.GetSection("AnonymousUserSettings"));

// Supabase JWT validation
var supabaseUrl = builder.Configuration["Supabase:Url"];
if (string.IsNullOrWhiteSpace(supabaseUrl))
    throw new InvalidOperationException(
        "Supabase:Url is not configured. Set the SUPABASE_URL environment variable.");
supabaseUrl = supabaseUrl.TrimEnd('/');

// Reject obviously malformed URLs (e.g. missing scheme, non-HTTP/HTTPS) before they
// produce a cryptic "issuer mismatch" 401 at runtime.
if (!Uri.TryCreate(supabaseUrl, UriKind.Absolute, out var supabaseUri) ||
    supabaseUri.Scheme is not ("http" or "https"))
    throw new InvalidOperationException(
        $"Supabase:Url '{supabaseUrl}' is not a valid absolute URL. " +
        "Expected format: https://<project-ref>.supabase.co");

// Use the JwtBearer Authority/OIDC discovery to fetch and cache JWKS lazily.
// The handler refreshes signing keys automatically, so Supabase JWT key
// rotation no longer requires a backend restart and a Supabase outage at
// boot no longer crashes the API.
var supabaseAuthority = $"{supabaseUrl}/auth/v1";
var requireHttpsMetadata = !builder.Environment.IsDevelopment()
                           || supabaseAuthority.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = supabaseAuthority;
        options.RequireHttpsMetadata = requireHttpsMetadata;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = supabaseAuthority,
            ValidAudience = "authenticated",
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub",
            RoleClaimType = "role"
        };

        options.Events = new JwtBearerEvents
        {
            // Log the specific failure reason so admins can distinguish
            // "Supabase URL wrong" (issuer mismatch) from "token expired"
            // from "OIDC discovery endpoint unreachable" without digging into
            // framework internals.
            OnAuthenticationFailed = ctx =>
            {
                var log = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                log.LogWarning(ctx.Exception,
                    "Supabase JWT validation failed ({ExceptionType}). " +
                    "Verify SUPABASE_URL matches your project and the JWT issuer is {Authority}.",
                    ctx.Exception.GetType().Name,
                    supabaseAuthority);
                return Task.CompletedTask;
            },

            // Return a structured JSON 401 so the frontend (and developers)
            // can tell our auth failure apart from a Supabase-side error.
            // Supabase GoTrue errors use {"code":5xx,"error_code":...}; ours
            // use {"succeeded":false,"error":"unauthorized","message":"..."}.
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                ctx.Response.ContentType = "application/json";

                var message = ctx.AuthenticateFailure is not null
                    ? $"JWT validation failed: {ctx.AuthenticateFailure.Message}"
                    : "Authentication required. Provide a valid Supabase access token.";

                await ctx.Response.WriteAsJsonAsync(new
                {
                    succeeded = false,
                    error = "unauthorized",
                    message
                });
            }
        };
    });

// Global FallbackPolicy: every endpoint requires either an authenticated
// principal or an anonymous-user GUID (resolved by AnonymousUserMiddleware).
// Endpoints that must remain open use [AllowAnonymous]. This closes the IDOR
// hole where a new controller could ship without its hand-rolled identity
// check and silently expose data to unauthenticated callers.
builder.Services.AddSingleton<IAuthorizationHandler, IdentifiedUserAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .AddRequirements(new IdentifiedUserRequirement())
        .Build();
});

// Per-IP-or-identity rate limiter for AI endpoints. The "ai" policy is applied
// to import / chat / tailor via [EnableRateLimiting("ai")]. Caps requests at
// 10 per minute per identity to make cost-DoS via the upstream HuggingFace /
// OpenAI quota impractical.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"error\":\"Too many AI requests. Please slow down and try again shortly.\"}",
            token);
    };

    options.AddPolicy("ai", httpContext =>
    {
        // Prefer identity over IP so that a single user behind a shared NAT
        // is not collectively throttled. Falls back to remote IP for the
        // (now blocked at the filter level) no-identity case.
        var key = httpContext.GetEffectiveUserId()
                  ?? httpContext.GetAnonymousUserId()?.ToString()
                  ?? httpContext.Connection.RemoteIpAddress?.ToString()
                  ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });
    });
});

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin) || !Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                    return false;

                // Local frontend development
                if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) && (uri.Port == 5173 || uri.Port == 7227))
                    return true;

                // Known production Vercel domain
                if (uri.Host.Equals("resume-spy-web.vercel.app", StringComparison.OrdinalIgnoreCase))
                    return true;

                // Custom subdomain
                if (uri.Host.Equals("resumespy.feifeijin.com", StringComparison.OrdinalIgnoreCase))
                    return true;

                // Vercel preview domains for this project/user namespace
                if (uri.Host.EndsWith("-feifeijins-projects.vercel.app", StringComparison.OrdinalIgnoreCase))
                    return true;

                return false;
            })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()); // Add this for better cross-origin support
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.UseRequestResponseLogging();

// Security headers run before the rest of the pipeline so even short-circuited
// responses (CORS preflight, rate-limit rejections, auth challenges) carry
// nosniff / frame-deny / CSP. HSTS is production-only because the dev cert is
// trusted only on localhost and pinning HSTS there would lock developers out
// of plain-HTTP tools.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseSecurityHeaders();

// Configure the HTTP request pipeline.
app.UseCors("AllowSpecificOrigin");

app.UseStaticFiles(); // Serve static files from wwwroot
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ResumeSpy API v1");
        options.DocumentTitle = "ResumeSpy API Documentation";
    });

    app.Use(async (context, next) =>
    {
        if (context.Request.Path.Equals("/", StringComparison.OrdinalIgnoreCase) ||
            context.Request.Path.Equals("/swagger.html", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect("/swagger/index.html", permanent: false);
            return;
        }

        await next();
    });
}

app.UseAuthentication();
app.UseEnsureLocalUser();
app.UseAnonymousUserMiddleware();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.MapGet("/", () => Results.Ok(new { service = "ResumeSpy API", status = "ok" }))
    .AllowAnonymous();

// Liveness: no dependency checks — confirms only that the process is up.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false
}).AllowAnonymous();

// Readiness: includes the DB check so monitors can distinguish a process
// that is up from one that can actually serve requests.
app.MapHealthChecks("/health/db", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db")
}).AllowAnonymous();

// Auth readiness: confirms our backend can reach Supabase's OIDC discovery
// endpoint. Unhealthy here means JWT validation will fail for all requests.
app.MapHealthChecks("/health/auth", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("auth")
}).AllowAnonymous();

app.Run();


