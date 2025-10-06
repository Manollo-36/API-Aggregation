using Microsoft.AspNetCore.Mvc;
using ApiAggregationService.Interfaces;
using ApiAggregationService.Models;
using Microsoft.AspNetCore.Authorization;
using System;

namespace ApiAggregationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IJwtService _jwtService;

        public AuthController(IJwtService jwtService)
        {
            _jwtService = jwtService;
        }

        /// <summary>
        /// Authenticate user and get JWT token
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>JWT token if authentication successful</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { message = "Username and password are required." });
                }

                if (!_jwtService.ValidateCredentials(request.Username, request.Password))
                {
                    return Unauthorized(new { message = "Invalid username or password." });
                }

                var user = _jwtService.GetUser(request.Username);
                var token = _jwtService.GenerateToken(user);

                return Ok(new LoginResponse
                {
                    Token = token,
                    Expiration = DateTime.UtcNow.AddMinutes(60),
                    Username = user.Username,
                    Role = user.Role
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during authentication.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get current user information (requires authentication)
        /// </summary>
        /// <returns>Current user details</returns>
        [HttpGet("me")]
        [Authorize]
        public ActionResult GetCurrentUser()
        {
            var username = User.Identity?.Name;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            return Ok(new
            {
                Username = username,
                Role = role,
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false
            });
        }

        /// <summary>
        /// Test endpoint for admin role (requires Admin role)
        /// </summary>
        /// <returns>Admin-only message</returns>
        [HttpGet("admin-only")]
        [Authorize(Roles = "Admin")]
        public ActionResult GetAdminData()
        {
            return Ok(new { message = "This is admin-only data.", timestamp = DateTime.UtcNow });
        }
    }
}