using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApiAggregationService.Interfaces;
using ApiAggregationService.Models;
using System.Linq; // Add this line

namespace ApiAggregationService.Controllers
{
    [ApiController]
    [Route("api/aggregation")]
    public class AggregationController : ControllerBase
    {
        private readonly IAggregationService _aggregationService;

        public AggregationController(IAggregationService aggregationService)
        {
            _aggregationService = aggregationService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return Ok("API Aggregation Service is running. Use /api/aggregation/aggregated-data for data.");
        }

        [HttpGet("aggregated-data")]
        public async Task<ActionResult<AggregatedWeatherData>> GetAggregatedData(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] string filter = null,
            [FromQuery] string sort = null)
        {
            try
            {
                var apiUrls = new List<string>
                {
                    $"https://api.openweathermap.org/data/3.0/onecall?lat={latitude}&lon={longitude}&appid=YOUR_API_KEY",
                    $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current_weather=true",
                    $"https://api.weather3.com/data?lat={latitude}&lon={longitude}"
                };

                var aggregatedData = await _aggregationService.GetAggregatedWeatherDataAsync(apiUrls, 
                    filter: string.IsNullOrEmpty(filter) ? null : data => data.Temperature > double.Parse(filter), 
                    orderBy:(Func<IEnumerable<AggregatedWeatherData>, IOrderedEnumerable<AggregatedWeatherData>>) (string.IsNullOrEmpty(sort) ? null : data => sort == "asc" ? data.OrderBy(d => d.Temperature) : data.OrderByDescending(d => d.Temperature)));

                return Ok(aggregatedData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request.", error = ex.Message });
            }
        }
    }
}