# API Aggregation Service Documentation

## Overview
The API Aggregation Service is a .NET 9.0 web API that fetches weather data from multiple external weather APIs simultaneously and provides aggregated results. The service supports filtering, sorting, and caching for improved performance.

## Table of Contents
1. [Setup and Configuration](#setup-and-configuration)
2. [API Endpoints](#api-endpoints)
3. [Input/Output Formats](#inputoutput-formats)
4. [Configuration](#configuration)
5. [Error Handling](#error-handling)
6. [Dependencies](#dependencies)
7. [Running the Application](#running-the-application)
8. [Testing](#testing)
9. [Performance Considerations](#performance-considerations)
10. [Troubleshooting](#troubleshooting)
11. [Security Considerations](#security-considerations)

## Setup and Configuration

### Prerequisites
- .NET 9.0 SDK
- Visual Studio Code or Visual Studio
- Internet connection (for external API calls)

### Installation
1. Clone or download the project
2. Navigate to the project directory:
   ```bash
   cd "API Aggregation/src/ApiAggregationService"
   ```
3. Restore NuGet packages:
   ```bash
   dotnet restore
   ```
4. Build the project:
   ```bash
   dotnet build
   ```

### Configuration Files

#### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*",
  "WeatherApi": {
    "OpenWeatherApiKey": "your_openweather_api_key_here"
  },
  "WeatherApi2": {      
    "WeatherStackApiKey": "your_weatherstack_api_key_here"
  },  
  "CacheSettings": {
    "Duration": 60
  }
}
```

**Required API Keys:**
- **OpenWeatherMap API Key**: Register at https://openweathermap.org/api
- **WeatherStack API Key**: Register at https://weatherstack.com/

## API Endpoints

### 1. Health Check Endpoint

#### `GET /`
Returns a simple health check message.

**Response:**
```json
"API Aggregation Service is running. Use /api/aggregation?latitude=<ENTER LATITUDE>&longitude=<ENTER LONGITUDE> for data."
```

### 2. Aggregated Weather Data Endpoint

#### `GET /api/aggregation`
Fetches weather data from multiple external APIs and returns aggregated results.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `latitude` | double | Yes | Latitude coordinate (-90 to 90) |
| `longitude` | double | Yes | Longitude coordinate (-180 to 180) |
| `filter` | string | No | Temperature threshold for filtering results |
| `sort` | string | No | Sort order: "asc" or "desc" |

**Example Requests:**
```bash
# Basic request
GET /api/aggregation?latitude=40.7128&longitude=-74.0060

# With filtering (temperature > 20)
GET /api/aggregation?latitude=40.7128&longitude=-74.0060&filter=20

# With sorting (ascending temperature)
GET /api/aggregation?latitude=40.7128&longitude=-74.0060&sort=asc

# Combined filtering and sorting
GET /api/aggregation?latitude=40.7128&longitude=-74.0060&filter=15&sort=desc
```

## Input/Output Formats

### Input Format
All inputs are provided as query parameters in the URL. No request body is required.

### Output Format

#### Success Response (200 OK)
```json
[
  {
    "main": {
      "temp": 22.5,
      "humidity": 65
    },
    "current_weather": null,
    "current": null
  },
  {
    "main": null,
    "current_weather": {
      "temperature": 21.8,
      "windspeed": 12.5,
      "humidity": 68
    },
    "current": null
  },
  {
    "main": null,
    "current_weather": null,
    "current": {
      "temperature": 23.1,
      "humidity": 62
    }
  }
]
```

#### Error Response (500 Internal Server Error)
```json
{
  "message": "An error occurred while processing your request.",
  "error": "Detailed error message here"
}
```

### Data Model Structure

#### AggregatedWeatherData
```csharp
public class AggregatedWeatherData
{
    public Main main { get; set; }
    public Current_weather current_weather { get; set; }
    public Current current { get; set; }

    public class Main
    {
        public double temp { get; set; }
        public double humidity { get; set; }
    }
    
    public class Current_weather
    {
        public double temperature { get; set; }
        public double windspeed { get; set; }
        public double humidity { get; set; }
    }
    
    public class Current
    {
        public double temperature { get; set; }
        public double humidity { get; set; }
    }
}
```

## Configuration

### External APIs Used
1. **OpenWeatherMap API**
   - Endpoint: `https://api.openweathermap.org/data/2.5/weather`
   - Returns data in `main` property
   - Requires API key

2. **Open-Meteo API** (Free, no key required)
   - Endpoint: `https://api.open-meteo.com/v1/forecast`
   - Returns data in `current_weather` property

3. **WeatherStack API**
   - Endpoint: `http://api.weatherstack.com/current`
   - Returns data in `current` property
   - Requires API key

### Caching
- **Duration**: 60 seconds (configurable in `appsettings.json`)
- **Cache Key**: Based on API URLs
- **Implementation**: In-memory caching using `IMemoryCache`

### Middleware
- **Error Handling**: Global exception handling middleware
- **HTTPS Redirection**: Enabled in production environments
- **Swagger/OpenAPI**: Available in development mode at `/swagger`

## Error Handling

### Common Errors
1. **Invalid Coordinates**: Latitude/longitude out of valid range
2. **API Key Missing**: One or more required API keys not configured
3. **Network Errors**: External API unavailable or timeout
4. **Invalid JSON**: Malformed response from external API
5. **Rate Limiting**: External API rate limits exceeded

### Error Response Format
All errors return a consistent JSON structure:
```json
{
  "message": "User-friendly error message",
  "error": "Technical error details"
}
```

## Dependencies

### NuGet Packages
```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.NewtonsoftJson" Version="9.0.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="9.0.6" />
```

### Built-in Services
- `HttpClient` for external API calls
- `IMemoryCache` for caching responses
- `IConfiguration` for settings management
- `ILogger` for logging

## Running the Application

### Development Mode
```bash
dotnet run
```

The application will start on:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

### Production Mode
```bash
dotnet run --environment Production
```

### Using Docker (Optional)
Create a `Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ApiAggregationService.csproj", "."]
RUN dotnet restore "ApiAggregationService.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "ApiAggregationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ApiAggregationService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ApiAggregationService.dll"]
```

Build and run:
```bash
docker build -t api-aggregation-service .
docker run -p 8080:80 api-aggregation-service
```

## Testing

### Running Unit Tests
```bash
cd ../ApiAggregationService.Tests
dotnet test
```

### Test Coverage
The test suite includes:
- Unit tests for service layer
- Controller integration tests
- Model validation tests
- Error handling tests
- Edge case scenarios

### Manual Testing with Swagger
1. Run the application in development mode
2. Navigate to `https://localhost:5001/swagger`
3. Use the interactive API documentation to test endpoints

### Sample cURL Commands
```bash
# Test health endpoint
curl -X GET "https://localhost:5001/"

# Test weather aggregation
curl -X GET "https://localhost:5001/api/aggregation?latitude=40.7128&longitude=-74.0060"

# Test with filtering and sorting
curl -X GET "https://localhost:5001/api/aggregation?latitude=40.7128&longitude=-74.0060&filter=20&sort=asc"
```

## Performance Considerations

### Caching Strategy
- Responses are cached for 60 seconds to reduce external API calls
- Cache keys include location parameters to ensure accuracy
- Memory cache automatically handles eviction

### Parallel Processing
- All external API calls are made simultaneously using `Task.WhenAll`
- Improves response time significantly compared to sequential calls

### Rate Limiting Considerations
- Implement proper rate limiting for external API calls
- Consider upgrading to paid tiers for higher limits
- Monitor API usage to avoid unexpected charges

## Troubleshooting

### Common Issues

1. **"Unable to resolve service for type IMemoryCache"**
   - Ensure `builder.Services.AddMemoryCache();` is in Program.cs

2. **"API key not found" errors**
   - Verify API keys are correctly set in appsettings.json
   - Check for typos in configuration section names

3. **Network timeout errors**
   - Check internet connectivity
   - Verify external API endpoints are accessible
   - Consider increasing HttpClient timeout

4. **JSON deserialization errors**
   - External API response format may have changed
   - Check API documentation for updates
   - Verify response content in logs

### Logging
Enable detailed logging by modifying `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information"
    }
  }
}
```

## Security Considerations

1. **API Key Security**
   - Store API keys in environment variables or secure configuration
   - Never commit API keys to source control
   - Use user secrets in development: `dotnet user-secrets set "WeatherApi:OpenWeatherApiKey" "your-key"`

2. **HTTPS**
   - Always use HTTPS in production
   - Configure proper SSL certificates

3. **Rate Limiting**
   - Implement rate limiting to prevent abuse
   - Monitor API usage patterns

4. **Input Validation**
   - Validate latitude/longitude ranges
   - Sanitize filter and sort parameters

## Support and Contributing

For issues, questions, or contributions, please refer to the project repository or contact the development team.