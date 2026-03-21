using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ResumeSpy.Infrastructure.Data;
using ResumeSpy.UI.Extensions;
using ResumeSpy.UI.Middlewares;
using ResumeSpy.Infrastructure.Configuration;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.UI.Swagger;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PrimaryDbConnection");
if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException(
        "ConnectionStrings:PrimaryDbConnection is not configured. Set it in appsettings.Development.json (local) " +
        "or the ConnectionStrings__PrimaryDbConnection environment variable (deployed).");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

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

// Register ILogger service
builder.Services.AddLogging();


// Load translator settings from configuration
builder.Services.Configure<TranslatorSettings>(builder.Configuration.GetSection("TranslatorSettings"));
builder.Services.Configure<SupabaseSettings>(builder.Configuration.GetSection("Supabase"));
builder.Services.Configure<AnonymousUserSettings>(builder.Configuration.GetSection("AnonymousUserSettings"));

// Supabase JWT validation
var supabaseUrl = builder.Configuration["Supabase:Url"]
    ?? throw new InvalidOperationException("Supabase:Url is not configured.");

// Fetch JWKS from Supabase at startup for ES256 key validation
using var jwksHttpClient = new HttpClient();
var jwksJson = jwksHttpClient.GetStringAsync($"{supabaseUrl}/auth/v1/.well-known/jwks.json").GetAwaiter().GetResult();
var jsonWebKeySet = new JsonWebKeySet(jwksJson);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = $"{supabaseUrl}/auth/v1",
            ValidAudience = "authenticated",
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub",
            RoleClaimType = "role",
            IssuerSigningKeys = jsonWebKeySet.GetSigningKeys()
        };
    });

builder.Services.AddAuthorization();

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
                if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) && uri.Port == 5173)
                    return true;

                // Known production Vercel domain
                if (uri.Host.Equals("resume-spy-web.vercel.app", StringComparison.OrdinalIgnoreCase))
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
// Configure the HTTP request pipeline.
app.UseCors("AllowSpecificOrigin");

app.UseStaticFiles(); // Serve static files from wwwroot
app.UseHttpsRedirection();

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

app.UseAuthentication();
app.UseEnsureLocalUser();
app.UseAnonymousUserMiddleware();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();


