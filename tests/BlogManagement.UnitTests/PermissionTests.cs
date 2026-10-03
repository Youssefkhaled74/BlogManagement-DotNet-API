using BlogManagement.Application.Security;
using Xunit;

namespace BlogManagement.UnitTests;

public sealed class PermissionTests
{
    [Fact]
    public void Permission_names_are_unique()
    {
        Assert.Equal(Permissions.All.Count, Permissions.All.Distinct().Count());
    }

    [Fact]
    public void Permission_names_use_resource_action_format()
    {
        Assert.All(Permissions.All, permission => Assert.Matches(@"^[a-z]+\.[a-z]+$", permission));
    }
}
