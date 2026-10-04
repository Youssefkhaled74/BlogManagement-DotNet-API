using System;
using System.Threading.Tasks;
using BlogManagement.Application.DTOs;
using BlogManagement.Api.Uploads;
using BlogManagement.Api.Localization;
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
            catch (InvalidOperationException ex) { return BadRequest(new { error = ApiMessages.Translate(ex.Message) }); }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var result = await _auth.LoginAsync(request);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { error = ApiMessages.Translate(ex.Message) }); }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
        {
            try
            {
                var result = await _auth.RefreshAsync(request.RefreshToken);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { error = ApiMessages.Translate(ex.Message) }); }
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
        public async Task<IActionResult> Me(CancellationToken ct)
        {
            var id = CurrentUserId;
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
            var username = User.FindFirst("username")?.Value;
            var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(x => x.Value).ToArray();
            var permissions = User.FindAll("permission").Select(x => x.Value).ToArray();
            var profile = await _users.GetAsync(id, ct);
            return Ok(new { id, email = profile.Email, username = profile.Username,
                profile.DisplayName, profile.ProfileImageUrl, roles, permissions });
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

        [Authorize, HttpPost("profile/image")]
        [Consumes("multipart/form-data"), RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<ActionResult<UserSummaryDto>> UploadProfileImage([FromForm] ImageUploadRequest request,
            [FromServices] ImageStorage images, CancellationToken ct)
        {
            var user = await _users.GetAsync(CurrentUserId, ct);
            var url = await images.SaveAsync(request.File, ct);
            var result = await _users.SetProfileImageAsync(CurrentUserId, url, ct);
            images.Delete(user.ProfileImageUrl);
            return Ok(result);
        }

        [Authorize, HttpDelete("profile/image")]
        public async Task<ActionResult<UserSummaryDto>> DeleteProfileImage([FromServices] ImageStorage images, CancellationToken ct)
        {
            var user = await _users.GetAsync(CurrentUserId, ct);
            var result = await _users.SetProfileImageAsync(CurrentUserId, null, ct);
            images.Delete(user.ProfileImageUrl);
            return Ok(result);
        }
    }
}
