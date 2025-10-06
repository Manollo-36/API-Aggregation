# API Aggregation Service Documentation

## Overview
The API Aggregation Service is a .NET 9.0 web API that fetches weather data from multiple external weather APIs simultaneously and provides aggregated results. The service supports filtering, sorting, caching, request statistics tracking, and **JWT-based authentication** for improved performance, monitoring, and security.

## Table of Contents
1. [Setup and Configuration](#setup-and-configuration)
2. [Authentication](#authentication)
3. [API Endpoints](#api-endpoints)
4. [Input/Output Formats](#inputoutput-formats)
5. [Configuration](#configuration)
6. [Error Handling](#error-handling)
7. [Dependencies](#dependencies)
8. [Running the Application](#running-the-application)
9. [Testing](#testing)
10. [Performance Considerations](#performance-considerations)
11. [Request Statistics](#request-statistics)
12. [Troubleshooting](#troubleshooting)
13. [Security Considerations](#security-considerations)

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
  },
  "JwtSettings": {
    "SecretKey": "Your-Very-Long-Secret-Key-For-JWT-Token-Generation-At-Least-32-Characters",
    "Issuer": "ApiAggregationService",
    "Audience": "ApiAggregationServiceUsers",
    "ExpirationMinutes": 60
  },
  "Users": [
    {
      "Username": "admin",
      "Password": "admin123",
      "Role": "Admin"
    },
    {
      "Username": "user",
      "Password": "user123",
      "Role": "User"
    }
  ]
}
```

**Required API Keys:**
- **OpenWeatherMap API Key**: Register at https://openweathermap.org/api
- **WeatherStack API Key**: Register at https://weatherstack.com/

**Default User Accounts:**
- **Admin**: username: `admin`, password: `admin123` (can access all endpoints)
- **User**: username: `user`, password: `user123` (can access weather data and statistics)

## Authentication

This API uses **JWT Bearer Authentication**. All endpoints (except login and health check) require a valid JWT token.

### Getting a Token

#### `POST /api/auth/login`
Authenticate with username/password to receive a JWT token.

**Request Body:**
```json
{
  "username": "admin",
  "password": "admin123"
}
```

**Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiration": "2024-01-01T13:00:00Z",
  "username": "admin",
  "role": "Admin"
}
```

**Response (401 Unauthorized):**
```json
{
  "message": "Invalid username or password."
}
```

### Using the Token

Include the token in the `Authorization` header for all subsequent requests:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### User Roles

- **Admin**: Full access to all endpoints including clearing statistics
- **User**: Access to weather data and viewing statistics (cannot clear statistics)

## API Endpoints

### 1. Health Check Endpoint (No Authentication Required)

#### `GET /`
Returns a simple health check message.

**Response:**
```json
"API Aggregation Service is running. Use /api/auth/login to authenticate, then access /api/aggregation for data."
```

### 2. Authentication Endpoints

#### `POST /api/auth/login` (No Authentication Required)
Authenticate and receive JWT token (see [Authentication](#authentication) section above).

#### `GET /api/auth/me` (Authentication Required)
Get current user information.

**Response:**
```json
{
  "username": "admin",
  "role": "Admin",
  "isAuthenticated": true
}
```

#### `GET /api/auth/admin-only` (Admin Role Required)
Test endpoint for admin-only access.

**Response:**
```json
{
  "message": "This is admin-only data.",
  "timestamp": "2024-01-01T12:00:00Z"
}
```

### 3. Aggregated Weather Data Endpoint (Authentication Required)

#### `GET /api/aggregation`
Fetches weather data from multiple external APIs and returns aggregated results.

**Headers:**
```
Authorization: Bearer <your-jwt-token>
```

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
Authorization: Bearer <token>

# With filtering (temperature > 20)
GET /api/aggregation?latitude=40.7128&longitude=-74.0060&filter=20
Authorization: Bearer <token>

# With sorting (ascending temperature)
GET /api/aggregation?latitude=40.7128&longitude=-74.0060&sort=asc
Authorization: Bearer <token>

# Combined filtering and sorting
GET /api/aggregation?latitude=40.7128&longitude=-74.0060&filter=15&sort=desc
Authorization: Bearer <token>
```

### 4. Statistics Endpoints (Authentication Required)

#### `GET /api/statistics`
Returns performance statistics for all external APIs.

**Headers:**
```
Authorization: Bearer <your-jwt-token>
```

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

**Headers:**
```
Authorization: Bearer <your-jwt-token>
```

#### `DELETE /api/statistics` (Admin Role Required)
Clears all collected statistics.

**Headers:**
```
Authorization: Bearer <your-jwt-token>
```

**Response:**
```json
{
  "message": "Statistics cleared successfully."
}
```

## Input/Output Formats

### Input Format
- **Authentication**: Username/password in JSON body for login
- **API Requests**: Query parameters in URL + JWT token in Authorization header
- **No request body** required for weather data endpoints

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
  }
]
```

#### Authentication Error Response (401 Unauthorized)
```json
{
  "message": "Invalid username or password."
}
```

#### Authorization Error Response (403 Forbidden)
```json
{
  "message": "Access denied. Admin role required."
}
```

#### Server Error Response (500 Internal Server Error)
```json
{
  "message": "An error occurred while processing your request.",
  "error": "Detailed error message here"
}
```

### Data Model Structure

#### LoginRequest
```csharp
public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}
```

#### LoginResponse
```csharp
public class LoginResponse
{
    public string Token { get; set; }
    public DateTime Expiration { get; set; }
    public string Username { get; set; }
    public string Role { get; set; }
}
```

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

### JWT Configuration
- **Token Expiration**: 60 minutes (configurable)
- **Algorithm**: HMAC SHA-256
- **Claims**: Username, Role, JWT ID, Issued At
- **Issuer/Audience**: Configurable for environment-specific validation

### Caching
- **Duration**: 60 seconds (configurable in `appsettings.json`)
- **Cache Key**: Based on API URLs and coordinates
- **Implementation**: In-memory caching using `IMemoryCache`

### Middleware
- **JWT Authentication**: Validates bearer tokens on protected endpoints
- **Role-based Authorization**: Enforces role requirements
- **Error Handling**: Global exception handling middleware
- **HTTPS Redirection**: Enabled in production environments
- **Swagger/OpenAPI**: Available in development mode at `/swagger` with JWT support

## Error Handling

### Common Errors
1. **Invalid Credentials**: Wrong username/password (401 Unauthorized)
2. **Missing Token**: No Authorization header (401 Unauthorized)
3. **Invalid Token**: Expired or malformed JWT (401 Unauthorized)
4. **Insufficient Permissions**: User lacks required role (403 Forbidden)
5. **Invalid Coordinates**: Latitude/longitude out of valid range (400 Bad Request)
6. **API Key Missing**: External API keys not configured (500 Internal Server Error)
7. **Network Errors**: External API unavailable or timeout (502 Bad Gateway)

### Error Response Format
All errors return a consistent JSON structure:
```json
{
  "message": "User-friendly error message",
  "error": "Technical error details (if applicable)"
}
```

## Dependencies

### NuGet Packages
```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.NewtonsoftJson" Version="9.0.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="9.0.6" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.1.2" />
```

### Built-in Services
- `HttpClient` for external API calls
- `IMemoryCache` for caching responses
- `IConfiguration` for settings management
- `ILogger` for logging
- `IApiStatisticsService` for request statistics tracking
- `IJwtService` for JWT token generation and validation

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
- Unit tests for service layer (AggregationService, ApiStatisticsService, JwtService)
- Controller integration tests (AggregationController, StatisticsController, AuthController)
- Authentication and authorization tests
- Model validation tests
- Error handling tests
- Edge case scenarios

### Manual Testing with Swagger
1. Run the application in development mode
2. Navigate to `https://localhost:5001/swagger`
3. Use the **Authorize** button in Swagger UI:
   - Click "Authorize"
   - Enter: `Bearer <your-jwt-token>`
   - Or use the login endpoint to get a token first
4. Use the interactive API documentation to test endpoints

### Sample cURL Commands

```bash
# 1. Login to get token
curl -X POST "https://localhost:5001/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'

# Response will contain the token
# {
#   "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
#   "expiration": "2024-01-01T13:00:00Z",
#   "username": "admin",
#   "role": "Admin"
# }

# 2. Test health endpoint (no auth required)
curl -X GET "https://localhost:5001/"

# 3. Test weather aggregation (replace <TOKEN> with actual token)
curl -X GET "https://localhost:5001/api/aggregation?latitude=40.7128&longitude=-74.0060" \
  -H "Authorization: Bearer <TOKEN>"

# 4. Test with filtering and sorting
curl -X GET "https://localhost:5001/api/aggregation?latitude=40.7128&longitude=-74.0060&filter=20&sort=asc" \
  -H "Authorization: Bearer <TOKEN>"

# 5. Test statistics endpoint
curl -X GET "https://localhost:5001/api/statistics" \
  -H "Authorization: Bearer <TOKEN>"

# 6. Clear statistics (admin only)
curl -X DELETE "https://localhost:5001/api/statistics" \
  -H "Authorization: Bearer <TOKEN>"

# 7. Test current user info
curl -X GET "https://localhost:5001/api/auth/me" \
  -H "Authorization: Bearer <TOKEN>"

# 8. Test admin-only endpoint
curl -X GET "https://localhost:5001/api/auth/admin-only" \
  -H "Authorization: Bearer <TOKEN>"
```

## Performance Considerations

### Caching Strategy
- Responses are cached for 60 seconds to reduce external API calls
- Cache keys include location parameters to ensure accuracy
- Memory cache automatically handles eviction

### JWT Token Management
- Stateless authentication reduces server memory usage
- Token expiration prevents long-lived access
- No need for server-side session storage

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
**Note**: Requires authentication. Only Admin users can clear statistics.

## Troubleshooting

### Common Issues

1. **"Unauthorized" errors**
   - Verify you've included the Authorization header: `Authorization: Bearer <token>`
   - Check if the token has expired (60 minutes default)
   - Ensure you're using the correct username/password for login

2. **"Forbidden" errors**
   - Check if your user role has permission for the endpoint
   - Admin-only endpoints require Admin role
   - Verify the role claim in your JWT token

3. **"Unable to resolve service for type IJwtService"**
   - Ensure `builder.Services.AddScoped<IJwtService, JwtService>();` is in Program.cs

4. **"Configuration file not found" errors**
   - Ensure you're running from the correct directory: `src/ApiAggregationService`
   - Verify appsettings.json exists in the project directory

5. **JWT token validation errors**
   - Check JWT secret key configuration
   - Verify issuer and audience settings match
   - Ensure the secret key is at least 32 characters

6. **External API errors**
   - Verify API keys are valid and active
   - Check if API keys have expired
   - Monitor rate limits for external APIs

### Logging
Enable detailed logging by modifying `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information",
      "Microsoft.AspNetCore.Authentication": "Debug"
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
│   │   ├── appsettings.json          ← Must be here with JWT config
│   │   ├── ApiAggregationService.csproj
│   │   ├── Controllers/
│   │   │   ├── AggregationController.cs
│   │   │   ├── StatisticsController.cs
│   │   │   └── AuthController.cs
│   │   ├── Services/
│   │   │   ├── AggregationService.cs
│   │   │   ├── ApiStatisticsService.cs
│   │   │   └── JwtService.cs
│   │   ├── Models/
│   │   └── Interfaces/
│   └── ApiAggregationService.Tests/
│       └── ...
└── README.md
```

## Security Considerations

1. **JWT Security**
   - Use strong, random secret keys (minimum 32 characters)
   - Store JWT secret in environment variables or Azure Key Vault in production
   - Implement token refresh mechanism for production use
   - Consider shorter token expiration times for sensitive operations

2. **Password Security**
   - **IMPORTANT**: The demo uses plain text passwords for simplicity
   - **Production**: Implement proper password hashing (bcrypt, Argon2, etc.)
   - Enforce strong password policies
   - Consider implementing password reset functionality

3. **API Key Security**
   - Store API keys in environment variables or secure configuration
   - Never commit API keys to source control
   - Use user secrets in development:
     ```bash
     dotnet user-secrets set "WeatherApi:OpenWeatherApiKey" "your-key"
     dotnet user-secrets set "WeatherApi2:WeatherStackApiKey" "your-key"
     dotnet user-secrets set "JwtSettings:SecretKey" "your-jwt-secret"
     ```

4. **HTTPS**
   - Always use HTTPS in production
   - Configure proper SSL certificates
   - Redirect HTTP to HTTPS

5. **Input Validation**
   - Validate all input parameters
   - Sanitize user inputs
   - Implement proper error handling without information disclosure

6. **Rate Limiting**
   - Implement rate limiting to prevent abuse
   - Monitor API usage patterns
   - Consider implementing user-specific rate limits

7. **CORS (if needed)**
   - Configure CORS policies for browser-based clients
   - Restrict allowed origins in production

## Support and Contributing

For issues, questions, or contributions, please refer to the project repository or contact the development team.

### Quick Start Checklist

- [ ] Install .NET 9.0 SDK
- [ ] Clone the repository
- [ ] Navigate to `src/ApiAggregationService`
- [ ] Create `appsettings.json` with your API keys and JWT settings
- [ ] Run `dotnet restore`
- [ ] Run `dotnet build`
- [ ] Run `dotnet run`
- [ ] Test authentication at `https://localhost:5001/swagger`
- [ ] Login with admin/admin123 or user/user123
- [ ] Use the JWT token to access protected endpoints

### Example Complete Setup

```bash
# Using user secrets (recommended for development)
cd "src/ApiAggregationService"
dotnet user-secrets init
dotnet user-secrets set "WeatherApi:OpenWeatherApiKey" "your_openweather_key"
dotnet user-secrets set "WeatherApi2:WeatherStackApiKey" "your_weatherstack_key"
dotnet user-secrets set "JwtSettings:SecretKey" "Your-Very-Long-Secret-Key-At-Least-32-Characters-For-Production"
```

### Testing Workflow

1. **Start the application**:
   ```bash
   dotnet run
   ```

2. **Get JWT token**:
   ```bash
   curl -X POST "https://localhost:5001/api/auth/login" \
     -H "Content-Type: application/json" \
     -d '{"username":"admin","password":"admin123"}'
   ```

3. **Use the token for protected endpoints**:
   ```bash
   curl -X GET "https://localhost:5001/api/aggregation?latitude=40.7128&longitude=-74.0060" \
     -H "Authorization: Bearer <your-token-here>"
   ```

4. **Access Swagger UI** with JWT support: `https://localhost:5001/swagger`

Your API is now secured with JWT authentication while maintaining all the weather aggregation and statistics functionality!