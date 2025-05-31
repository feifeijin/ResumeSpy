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
    options.UseSqlServer(builder.Configuration.GetConnectionString("PrimaryDbConnection"));
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
            .WithOrigins("http://localhost:5173") // Replace with your allowed origin(s)
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Use the CORS middleware
app.UseCors("AllowSpecificOrigin");

app.MapControllers();

app.Run();


