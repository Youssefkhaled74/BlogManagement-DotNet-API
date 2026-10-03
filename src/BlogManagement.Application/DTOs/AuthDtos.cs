using System;

namespace BlogManagement.Application.DTOs
{
    public record RegisterRequestDto(string Username, string Email, string Password);

    public record LoginRequestDto(string Email, string Password);

    public record AuthResultDto(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt, DateTime RefreshTokenExpiresAt);

    public record UserDto(Guid Id, string Username, string Email, bool IsActive);
}
