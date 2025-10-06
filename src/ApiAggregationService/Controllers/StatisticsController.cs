using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ApiAggregationService.Interfaces;
using ApiAggregationService.Models;
using System.Collections.Generic;
using System;

namespace ApiAggregationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for all endpoints
    public class StatisticsController : ControllerBase
    {
        private readonly IApiStatisticsService _statisticsService;

        public StatisticsController(IApiStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        /// <summary>
        /// Get statistics for all APIs (requires authentication)
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
        /// Get statistics for a specific API by name (requires authentication)
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
        /// Clear all statistics (requires Admin role)
        /// </summary>
        /// <returns>Success message</returns>
        [HttpDelete]
        [Authorize(Roles = "Admin")] // Only admins can clear statistics
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