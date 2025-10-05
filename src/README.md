# API Aggregation Service Documentation

## Overview
The API Aggregation Service is a .NET 9.0 web API that fetches weather data from multiple external weather APIs simultaneously and provides aggregated results. The service supports filtering, sorting, caching, and request statistics tracking for improved performance and monitoring.

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
10. [Request Statistics](#request-statistics)
11. [Troubleshooting](#troubleshooting)
12. [Security Considerations](#security-considerations)

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

### 3. Statistics Endpoints

#### `GET /api/statistics`
Returns performance statistics for all external APIs.

**Response:**
```json
[
  {
    "apiName": "OpenWeatherMap",
    "totalRequests": 15,
    "averageResponseTimeMs": 142.5,
    "performanceBuckets": {
      "fast": 5,    // < 100ms
      "average": 7, // 100-200ms  
      "slow": 3     // > 200ms
    }
  }
]
```

#### `GET /api/statistics/{apiName}`
Returns statistics for a specific API by name.

**Parameters:**
- `apiName`: Name of the API (e.g., "OpenWeatherMap", "OpenMeteo", "WeatherStack")

#### `DELETE /api/statistics`
Clears all collected statistics.

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

#### 401 Unauthorized Response
```json
{
  "message": "One or more API keys are invalid or have expired.",
  "error": "Response status code does not indicate success: 401 (Unauthorized)"
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

#### ApiStatistics
```csharp
public class ApiStatistics
{
    public string ApiName { get; set; }
    public int TotalRequests { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public PerformanceBuckets PerformanceBuckets { get; set; }
}

public class PerformanceBuckets
{
    public int Fast { get; set; }      // < 100ms
    public int Average { get; set; }   // 100-200ms
    public int Slow { get; set; }      // > 200ms
}
```

## Configuration

### External APIs Used
1. **OpenWeatherMap API**
   - Endpoint: `https://api.openweathermap.org/data/2.5/weather`
   - Returns data in `main` property
   - Requires API key
   - Rate limit: 1,000 calls/day (free tier)

2. **Open-Meteo API** (Free, no key required)
   - Endpoint: `https://api.open-meteo.com/v1/forecast`
   - Returns data in `current_weather` property
   - No rate limits

3. **WeatherStack API**
   - Endpoint: `http://api.weatherstack.com/current`
   - Returns data in `current` property
   - Requires API key
   - Rate limit: 1,000 calls/month (free tier)

### Caching
- **Duration**: 60 seconds (configurable in `appsettings.json`)
- **Cache Key**: Based on API URLs and coordinates
- **Implementation**: In-memory caching using `IMemoryCache`

### Middleware
- **Error Handling**: Global exception handling middleware
- **HTTPS Redirection**: Enabled in production environments
- **Swagger/OpenAPI**: Available in development mode at `/swagger`

## Error Handling

### Common Errors
1. **Invalid Coordinates**: Latitude/longitude out of valid range (400 Bad Request)
2. **API Key Missing**: One or more required API keys not configured (500 Internal Server Error)
3. **API Key Invalid**: External API returns 401 Unauthorized
4. **Network Errors**: External API unavailable or timeout (502 Bad Gateway)
5. **Invalid JSON**: Malformed response from external API (500 Internal Server Error)
6. **Rate Limiting**: External API rate limits exceeded (429 Too Many Requests)

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
- `IApiStatisticsService` for request statistics tracking

## Running the Application

### Development Mode
```bash
# Navigate to project directory
cd "src/ApiAggregationService"

# Run the application
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
COPY ["src/ApiAggregationService/ApiAggregationService.csproj", "ApiAggregationService/"]
RUN dotnet restore "ApiAggregationService/ApiAggregationService.csproj"
COPY src/ApiAggregationService/ ApiAggregationService/
WORKDIR "/src/ApiAggregationService"
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
# From the root directory
docker build -t api-aggregation-service .
docker run -p 8080:80 api-aggregation-service
```

## Testing

### Running Unit Tests
```bash
cd "src/ApiAggregationService.Tests"
dotnet test
```

### Test Coverage
The test suite includes:
- Unit tests for service layer (AggregationService, ApiStatisticsService)
- Controller integration tests (AggregationController, StatisticsController)
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

# Test statistics endpoint
curl -X GET "https://localhost:5001/api/statistics"

# Clear statistics
curl -X DELETE "https://localhost:5001/api/statistics"
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

## Request Statistics

The service automatically tracks performance statistics for all external API calls:

### Performance Buckets
- **Fast**: Response time < 100ms
- **Average**: Response time 100-200ms
- **Slow**: Response time > 200ms

### Metrics Collected
- Total number of requests per API
- Average response time per API
- Request distribution across performance buckets
- Success/failure rates

### Accessing Statistics
Use the `/api/statistics` endpoints to view performance data and monitor API health.

## Troubleshooting

### Common Issues

1. **"Unable to resolve service for type IMemoryCache"**
   - Ensure `builder.Services.AddMemoryCache();` is in Program.cs

2. **"API key not found" errors**
   - Verify API keys are correctly set in appsettings.json
   - Check for typos in configuration section names

3. **"Configuration file not found" errors**
   - Ensure you're running from the correct directory: `src/ApiAggregationService`
   - Verify appsettings.json exists in the project directory

4. **Network timeout errors**
   - Check internet connectivity
   - Verify external API endpoints are accessible
   - Consider increasing HttpClient timeout

5. **JSON deserialization errors**
   - External API response format may have changed
   - Check API documentation for updates
   - Verify response content in logs

6. **401 Unauthorized errors**
   - Verify API keys are valid and active
   - Check if API keys have expired
   - Ensure proper API key format

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

### Directory Structure
Ensure your project structure matches:
```
API Aggregation/
├── src/
│   ├── ApiAggregationService.sln
│   ├── ApiAggregationService/
│   │   ├── Program.cs
│   │   ├── appsettings.json          ← Must be here
│   │   ├── ApiAggregationService.csproj
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Models/
│   │   └── Interfaces/
│   └── ApiAggregationService.Tests/
│       └── ...
└── README.md
```

## Security Considerations

1. **API Key Security**
   - Store API keys in environment variables or secure configuration
   - Never commit API keys to source control
   - Use user secrets in development: 
     ```bash
     dotnet user-secrets set "WeatherApi:OpenWeatherApiKey" "your-key"
     dotnet user-secrets set "WeatherApi2:WeatherStackApiKey" "your-key"
     ```

2. **HTTPS**
   - Always use HTTPS in production
   - Configure proper SSL certificates

3. **Rate Limiting**
   - Implement rate limiting to prevent abuse
   - Monitor API usage patterns

4. **Input Validation**
   - Validate latitude/longitude ranges
   - Sanitize filter and sort parameters

5. **Error Information Disclosure**
   - Avoid exposing sensitive information in error messages
   - Log detailed errors server-side only

## Support and Contributing

For issues, questions, or contributions, please refer to the project repository or contact the development team.

### Quick Start Checklist

- [ ] Install .NET 9.0 SDK
- [ ] Clone the repository
- [ ] Navigate to `src/ApiAggregationService`
- [ ] Create `appsettings.json` with your API keys
- [ ] Run `dotnet restore`
- [ ] Run `dotnet build`
- [ ] Run `dotnet run`
- [ ] Test at `https://localhost:5001/swagger`

### Example API Keys Setup

```bash
# Using user secrets (recommended for development)
cd "src/ApiAggregationService"
dotnet user-secrets init
dotnet user-secrets set "WeatherApi:OpenWeatherApiKey" "your_openweather_key"
dotnet user-secrets set "WeatherApi2:WeatherStackApiKey" "your_weatherstack_key"
```