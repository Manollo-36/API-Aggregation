# API Aggregation Service

## Overview
The API Aggregation Service is a .NET-based application built using ASP.NET Core that aggregates weather data from multiple external APIs. It provides a unified endpoint for retrieving aggregated data with options for filtering and sorting. The service also includes performance statistics for API requests.

## Features
- Fetches data from multiple external weather APIs simultaneously.
- Provides an endpoint for retrieving aggregated weather data.
- Supports filtering and sorting of aggregated results.
- Implements error handling middleware for robust API responses.
- Tracks and provides performance metrics for API requests.

## Project Structure
```
ApiAggregationService
├── src
│   ├── ApiAggregationService
│   │   ├── Controllers
│   │   │   ├── AggregationController.cs
│   │   │   └── StatisticsController.cs
│   │   ├── Interfaces
│   │   │   └── IAggregationService.cs
│   │   ├── Middleware
│   │   │   └── ErrorHandlingMiddleware.cs
│   │   ├── Models
│   │   │   ├── AggregatedWeatherData.cs
│   │   │   └── ApiResponse.cs
│   │   ├── Services
│   │   │   ├── AggregationService.cs
│   │   │   └── StatisticsService.cs
│   │   ├── Program.cs
│   │   ├── Startup.cs
│   │   └── appsettings.json
│   └── ApiAggregationService.Tests
│       ├── AggregationServiceTests.cs
│       └── StatisticsServiceTests.cs
├── .gitignore
└── README.md
```

## Setup Instructions
1. Clone the repository:
   ```
   git clone <repository-url>
   ```
2. Navigate to the project directory:
   ```
   cd ApiAggregationService/src/ApiAggregationService
   ```
3. Restore the dependencies:
   ```
   dotnet restore
   ```
4. Run the application:
   ```
   dotnet run
   ```

## API Endpoints

### Aggregation Endpoint
- **URL:** `/api/aggregation`
- **Method:** `GET`
- **Parameters:**
  - `latitude`: Latitude for weather data.
  - `longitude`: Longitude for weather data.
  - `filter`: Optional filter for aggregated data.
  - `sort`: Optional sorting criteria for aggregated data.

### Statistics Endpoint
- **URL:** `/api/statistics`
- **Method:** `GET`
- **Description:** Returns performance metrics for API requests.

## Input/Output Formats
- **Input:** Query parameters as specified in the API endpoints.
- **Output:** JSON format containing aggregated weather data or performance metrics.

## Error Handling
The service includes middleware for global error handling, ensuring that appropriate error responses are returned to the client in case of exceptions.

## Testing
Unit tests are provided for both the AggregationService and StatisticsService to ensure the correctness of methods and performance metrics.

## Caching
The service implements caching strategies to improve performance and reduce the load on external APIs.
