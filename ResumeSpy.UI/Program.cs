using Microsoft.EntityFrameworkCore;
using ResumeSpy.Infrastructure.Data;
using ResumeSpy.UI.Extensions;
using ResumeSpy.UI.Middlewares;
using ResumeSpy.UI.Interfaces;
using ResumeSpy.UI.Services;
using ResumeSpy.UI.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("PrimaryDbConnection"));
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

builder.Services.AddControllersWithViews();
builder.Services.RegisterService();

// Add caching services
builder.Services.AddMemoryCache();

// Register ILogger service
builder.Services.AddLogging();


// Load translator settings from configuration
builder.Services.Configure<TranslatorSettings>(builder.Configuration.GetSection("TranslatorSettings"));

// Register the factory
builder.Services.AddSingleton<TranslatorFactory>();

// Register the translator service using the factory
builder.Services.AddScoped<ITranslator>(provider =>
{
    var factory = provider.GetRequiredService<TranslatorFactory>();
    return factory.CreateTranslator();
});

builder.Services.AddScoped<TranslationService>();

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder
            .WithOrigins(
                "http://localhost:5173",    // Frontend (HTTP)
                "https://localhost:5173",   // Frontend (HTTPS)
                "http://localhost:5293",    // API (HTTP)
                "https://localhost:7227"    // API (HTTPS)
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()); // Add this for better cross-origin support
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Use the CORS middleware BEFORE other middleware
app.UseCors("AllowSpecificOrigin");

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();


