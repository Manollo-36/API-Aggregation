using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ApiAggregationService.Interfaces;
using ApiAggregationService.Services;
using ApiAggregationService.Models;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;

// Handle debugging directory issue
var currentDir = Directory.GetCurrentDirectory();
//Console.WriteLine($"Current Directory: {currentDir}");

if (currentDir.EndsWith("src") && !File.Exists("appsettings.json"))
{
    var projectDir = Path.Combine(currentDir, "ApiAggregationService");
    if (Directory.Exists(projectDir) && File.Exists(Path.Combine(projectDir, "appsettings.json")))
    {
        Directory.SetCurrentDirectory(projectDir);
        //Console.WriteLine($"Changed directory to: {Directory.GetCurrentDirectory()}");
    }
}

var builder = WebApplication.CreateBuilder(args);

// Configure JWT settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Add services to the container
builder.Services.AddControllers()
    .AddNewtonsoftJson();

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// Register your services
builder.Services.AddScoped<IAggregationService, AggregationService>();
builder.Services.AddSingleton<IApiStatisticsService, ApiStatisticsService>();
builder.Services.AddScoped<IJwtService, JwtService>();

// Add Swagger/OpenAPI with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "API Aggregation Service", 
        Version = "v1",
        Description = "A secure weather data aggregation service with JWT authentication"
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Configure HTTPS redirection
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
    options.HttpsPort = 5001;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // In development, you might want to disable HTTPS redirection
    // app.UseHttpsRedirection();
}
else
{
    app.UseHttpsRedirection();
}

// Add authentication and authorization middleware (order matters!)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Add a simple home endpoint (no authentication required)
app.MapGet("/", () => "API Aggregation Service is running. Use /api/auth/login to authenticate, then access /api/aggregation for data.")
    .WithTags("Health");

// Explicitly specify the URLs to listen on
app.Urls.Add("http://localhost:5000");
app.Urls.Add("https://localhost:5001");

app.Run();

public partial class Program { }
