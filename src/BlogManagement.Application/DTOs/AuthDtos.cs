using System.ComponentModel.DataAnnotations;

namespace BlogManagement.Application.DTOs;

public sealed record RegisterRequestDto(
    [Required, MinLength(2), MaxLength(80)] string Username,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8), MaxLength(100)] string Password);

public sealed record LoginRequestDto(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record RefreshTokenRequestDto([Required] string RefreshToken);

public sealed record ChangePasswordDto(
    [Required] string CurrentPassword,
    [Required, MinLength(8), MaxLength(100)] string NewPassword);

public sealed record AuthResultDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt,
    UserSummaryDto User);

public sealed record UserDto(Guid Id, string Username, string Email, bool IsActive);
