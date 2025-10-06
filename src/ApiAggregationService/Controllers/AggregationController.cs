using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Net.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApiAggregationService.Interfaces;
using ApiAggregationService.Models;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Reflection.Metadata.Ecma335;

namespace ApiAggregationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // Require authentication for all endpoints
    [Authorize]
    public class AggregationController : ControllerBase
    {
        private readonly IAggregationService _aggregationService;
        private readonly IConfiguration _configuration;

        public AggregationController(IAggregationService aggregationService, IConfiguration configuration)
        {
            _aggregationService = aggregationService;
            _configuration = configuration;
        }

        /// Get aggregated weather data from multiple APIs (requires authentication)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AggregatedWeatherData>>> GetAggregatedData(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] string filter = null,
            [FromQuery] string sort = null)
        {
            try
            {
                if (latitude == 0 || longitude == 0)
                {
                    return BadRequest(new { message = "Latitude and Longitude are required query parameters." });
                }
                else if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
                {
                    return BadRequest(new { message = "Invalid latitude or longitude values." });
                }
                else
                {
                    // Get API keys from configuration
                    string openWeatherApiKey = _configuration["WeatherApi:OpenWeatherApiKey"];
                    string weatherStackApiKey = _configuration["WeatherApi2:WeatherStackApiKey"];

                    // Validate API keys
                    if (string.IsNullOrEmpty(openWeatherApiKey))
                    {
                        return StatusCode(500, new { message = "OpenWeather API key is not configured." });
                    }
                    if (string.IsNullOrEmpty(weatherStackApiKey))
                    {
                        return StatusCode(500, new { message = "WeatherStack API key is not configured." });
                    }

                    // Build API URLs - only include APIs with valid keys
                    var apiUrls = new List<string>();

                    // OpenWeatherMap (requires key)
                    if (!string.IsNullOrEmpty(openWeatherApiKey))
                    {
                        apiUrls.Add($"https://api.openweathermap.org/data/2.5/weather?lat={latitude}&lon={longitude}&appid={openWeatherApiKey}&units=metric");
                    }

                    // Open-Meteo (no key required)
                    apiUrls.Add($"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current_weather=true");

                    // WeatherStack (requires key)
                    if (!string.IsNullOrEmpty(weatherStackApiKey))
                    {
                        apiUrls.Add($"http://api.weatherstack.com/current?access_key={weatherStackApiKey}&query={latitude},{longitude}");
                    }

                    if (!apiUrls.Any())
                    {
                        return StatusCode(500, new { message = "No valid API configurations found." });
                    }

                    // Define filter function if provided
                    Func<AggregatedWeatherData, bool> filterFunc = null;
                    if (!string.IsNullOrEmpty(filter) && double.TryParse(filter, out double filterValue))
                    {
                        filterFunc = data => data.main?.temp > filterValue ||
                                            data.current_weather?.temperature > filterValue ||
                                            data.current?.temperature > filterValue;
                    }

                    // Define sort function if provided
                    Func<IEnumerable<AggregatedWeatherData>, IOrderedEnumerable<AggregatedWeatherData>> sortFunc = null;
                    if (!string.IsNullOrEmpty(sort))
                    {
                        if (sort.ToLower() == "asc")
                        {
                            sortFunc = data => data.OrderBy(d => d.main?.temp ?? d.current_weather?.temperature ?? d.current?.temperature ?? 0);
                        }
                        else if (sort.ToLower() == "desc")
                        {
                            sortFunc = data => data.OrderByDescending(d => d.main?.temp ?? d.current_weather?.temperature ?? d.current?.temperature ?? 0);
                        }
                    }

                    // Use the service to get aggregated data
                    var result = await _aggregationService.GetAggregatedWeatherDataAsync(apiUrls, filterFunc, sortFunc);

                    return Ok(result);
                }
            }
            catch (HttpRequestException httpEx) when (httpEx.Message.Contains("401"))
            {
                return StatusCode(401, new { message = "One or more API keys are invalid or have expired.", error = httpEx.Message });
            }
            catch (HttpRequestException httpEx)
            {
                return StatusCode(502, new { message = "Error communicating with external APIs.", error = httpEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request.", error = ex.Message });
            }
        }
    }
}