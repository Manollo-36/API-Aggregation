using Microsoft.AspNetCore.Mvc;

namespace ApiAggregationService.Controllers
{
    [ApiController]
    [Route("/")]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("API Aggregation Service is running. Use /api/aggregation/aggregated-data for data.");
        }
    }
}