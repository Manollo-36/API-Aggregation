using Microsoft.AspNetCore.Mvc;
using ApiAggregationService.Services;

namespace ApiAggregationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatisticsController : ControllerBase
    {
        private readonly ApiStatisticsService _statisticsService;

        public StatisticsController(ApiStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        [HttpGet]
        [Route("GetStatistics")]
        public IActionResult GetStatistics()
        {
            var statistics = _statisticsService.GetAllStats();
            return Ok(statistics);
        }
    }
}