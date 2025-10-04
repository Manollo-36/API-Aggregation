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
        public async Task<ActionResult<AggregatedWeatherData>> GetAggregatedData(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] string filter = null,
            [FromQuery] string sort = null)
        {
            try
            {
                string openWeatherApiKey = _configuration.GetValue<string>("WeatherApi:OpenWeatherApiKey");
                string weatherStackApiKey = _configuration.GetValue<string>("WeatherApi2:WeatherStackApiKey");

                var openWeatherUrl = $"https://api.openweathermap.org/data/2.5/weather?lat={latitude}&lon={longitude}&appid={openWeatherApiKey}&units=metric";
                var openMeteoUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current_weather=true";
                var weatherStackUrl = $"http://api.weatherstack.com/current?access_key={weatherStackApiKey}&query={latitude},{longitude}";

                using var httpClient = new HttpClient();

                // Start all requests simultaneously
                var openWeatherTask = httpClient.GetStringAsync(openWeatherUrl);
                var openMeteoTask = httpClient.GetStringAsync(openMeteoUrl);
                var weatherStackTask = httpClient.GetStringAsync(weatherStackUrl);

                await Task.WhenAll(openWeatherTask, openMeteoTask, weatherStackTask);

                // Parse responses
                var openWeatherData =  JsonConvert.DeserializeObject<AggregatedWeatherData>(openWeatherTask.Result);
                var openMeteoData = JsonConvert.DeserializeObject<AggregatedWeatherData>(openMeteoTask.Result);
                var weatherStackData = JsonConvert.DeserializeObject<AggregatedWeatherData>(weatherStackTask.Result);

                var aggregatedList = new List<AggregatedWeatherData>
                {
                    openWeatherData,
                    openMeteoData,
                    weatherStackData
                };

                // Optional: Apply filter and sorting
                if (!string.IsNullOrEmpty(filter) && double.TryParse(filter, out double filterValue))
                {
                    aggregatedList = aggregatedList.Where(d => d.main.temp > filterValue).ToList();
                }

                if (!string.IsNullOrEmpty(sort))
                {
                    aggregatedList = sort == "asc"
                        ? aggregatedList.OrderBy(d => d.main.temp).ToList()
                        : aggregatedList.OrderByDescending(d => d.main.temp).ToList();
                }

                // returning the first as an example
                return Ok(aggregatedList);

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request.", error = ex.Message });
            }
        }
    }
}