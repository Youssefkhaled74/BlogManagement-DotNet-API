using System.Globalization;
using BlogManagement.Api.Localization;
using Xunit;

namespace BlogManagement.IntegrationTests;

public sealed class ApiMessagesTests
{
    [Fact]
    public void Arabic_translation_preserves_unrecognized_errors_and_translates_dynamic_errors()
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ar");
            Assert.Equal("Specific actionable error.", ApiMessages.Translate("Specific actionable error."));
            Assert.Equal("صلاحيات غير معروفة: blogs.invalid", ApiMessages.Translate("Unknown permissions: blogs.invalid"));
            Assert.Equal("جلسة الدخول غير صالحة. سجّل الدخول مرة أخرى.", ApiMessages.Translate("Authenticated user identifier is missing."));
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
            Assert.Equal("Invalid credentials.", ApiMessages.Translate("Invalid credentials."));
        }
        finally { CultureInfo.CurrentUICulture = original; }
    }
}
