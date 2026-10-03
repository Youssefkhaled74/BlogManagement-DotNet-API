using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace BlogManagement.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid CurrentUserId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return Guid.TryParse(value, out var id)
                ? id
                : throw new UnauthorizedAccessException("Authenticated user identifier is missing.");
        }
    }

    protected bool HasPermission(string permission) => User.HasClaim("permission", permission);
}
