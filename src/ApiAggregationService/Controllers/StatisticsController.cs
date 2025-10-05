using Microsoft.AspNetCore.Mvc;
using ApiAggregationService.Interfaces;
using ApiAggregationService.Models;
using System.Collections.Generic;
using System;

namespace ApiAggregationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatisticsController : ControllerBase
    {
        private readonly IApiStatisticsService _statisticsService;

        public StatisticsController(IApiStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        /// <summary>
        /// Get statistics for all APIs
        /// </summary>
        /// <returns>Collection of API statistics</returns>
        [HttpGet]
        public ActionResult<IEnumerable<ApiStatistics>> GetAllStatistics()
        {
            try
            {
                var stats = _statisticsService.GetAllStats();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving statistics.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get statistics for a specific API by name
        /// </summary>
        /// <param name="apiName">Name of the API</param>
        /// <returns>API statistics</returns>
        [HttpGet("{apiName}")]
        public ActionResult<ApiStatistics> GetStatisticsByApiName(string apiName)
        {
            try
            {
                var stats = _statisticsService.GetStatsByApiName(apiName);
                if (stats == null)
                {
                    return NotFound(new { message = $"No statistics found for API: {apiName}" });
                }
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving statistics.", error = ex.Message });
            }
        }

        /// <summary>
        /// Clear all statistics
        /// </summary>
        /// <returns>Success message</returns>
        [HttpDelete]
        public ActionResult ClearStatistics()
        {
            try
            {
                _statisticsService.ClearStats();
                return Ok(new { message = "Statistics cleared successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while clearing statistics.", error = ex.Message });
            }
        }
    }
}