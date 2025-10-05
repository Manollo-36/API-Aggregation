using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using ApiAggregationService;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using ApiAggregationService.Interfaces;
using ApiAggregationService.Services;
using ApiAggregationService.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddNewtonsoftJson();

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// Register your services
builder.Services.AddScoped<IAggregationService, AggregationService>();
builder.Services.AddSingleton<IApiStatisticsService, ApiStatisticsService>(); // Add this line

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure HTTPS redirection
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
    options.HttpsPort = 5001; // Set your HTTPS port
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add error handling middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

// Only use HTTPS redirection in production, or configure it properly
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();

app.Run();

// Expose the Program class for integration testing
public partial class Program { }
