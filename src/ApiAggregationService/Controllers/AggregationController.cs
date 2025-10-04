using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApiAggregationService.Interfaces;
using ApiAggregationService.Models;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace ApiAggregationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AggregationController : ControllerBase
    {
        private readonly IAggregationService _aggregationService;
        private readonly IConfiguration _configuration;

        public AggregationController(IAggregationService aggregationService, IConfiguration configuration)
        {
            _aggregationService = aggregationService;
            _configuration = configuration;
        }
      
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AggregatedWeatherData>>> GetAggregatedData(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] string filter = null,
            [FromQuery] string sort = null)
        {
            try
            {
                // Get API keys from configuration
                string openWeatherApiKey = _configuration["WeatherApi:OpenWeatherApiKey"];
                string weatherStackApiKey = _configuration["WeatherApi2:WeatherStackApiKey"];

                // Build API URLs
                var apiUrls = new List<string>
                {
                    $"https://api.openweathermap.org/data/2.5/weather?lat={latitude}&lon={longitude}&appid={openWeatherApiKey}&units=metric",
                    $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current_weather=true",
                    $"http://api.weatherstack.com/current?access_key={weatherStackApiKey}&query={latitude},{longitude}"
                };

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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request.", error = ex.Message });
            }
        }
    }
}