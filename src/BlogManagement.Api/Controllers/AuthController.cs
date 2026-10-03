using System;
using System.Threading.Tasks;
using BlogManagement.Application.DTOs;
using BlogManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogManagement.Api.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : ApiControllerBase
    {
        private readonly IAuthService _auth;
        private readonly IUserService _users;

        public AuthController(IAuthService auth, IUserService users)
        {
            _auth = auth;
            _users = users;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            try
            {
                var result = await _auth.RegisterAsync(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var result = await _auth.LoginAsync(request);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { error = ex.Message }); }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
        {
            try
            {
                var result = await _auth.RefreshAsync(request.RefreshToken);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { error = ex.Message }); }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request)
        {
            await _auth.LogoutAsync(request.RefreshToken);
            return NoContent();
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            var id = CurrentUserId;
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
            var username = User.FindFirst("username")?.Value;
            var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(x => x.Value).ToArray();
            var permissions = User.FindAll("permission").Select(x => x.Value).ToArray();
            return Ok(new { id, email, username, roles, permissions });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request, CancellationToken ct)
        {
            await _auth.ChangePasswordAsync(CurrentUserId, request, ct);
            return NoContent();
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<ActionResult<UserSummaryDto>> UpdateProfile([FromBody] UpdateUserDto request, CancellationToken ct)
        {
            return Ok(await _users.UpdateAsync(CurrentUserId, request, CurrentUserId, ct));
        }
    }
}
