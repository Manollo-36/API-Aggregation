using Microsoft.AspNetCore.Mvc;
using ApiAggregationService.Services;

namespace ApiAggregationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatisticsController : ControllerBase
    {
        private readonly StatisticsService _statisticsService;

        public StatisticsController(StatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        [HttpGet]
        [Route("GetStatistics")]
        public IActionResult GetStatistics()
        {
            var statistics = _statisticsService.GetStatistics();
            return Ok(statistics);
        }
    }
}