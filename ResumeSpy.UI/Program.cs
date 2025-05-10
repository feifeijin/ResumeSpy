using ResumeSpy.UI.Interfaces;
using ResumeSpy.UI.Services;
using ResumeSpy.UI.Configuration;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

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


