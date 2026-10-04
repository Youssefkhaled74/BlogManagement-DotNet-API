using BlogManagement.Api.Uploads;
using Xunit;

namespace BlogManagement.IntegrationTests;

public sealed class ImageStorageTests
{
    [Fact]
    public void Rejects_svg_html_and_empty_files()
    {
        Assert.Throws<InvalidOperationException>(() => ImageStorage.DetectExtension([]));
        Assert.Throws<InvalidOperationException>(() => ImageStorage.DetectExtension(System.Text.Encoding.UTF8.GetBytes("<svg></svg>")));
        Assert.Throws<InvalidOperationException>(() => ImageStorage.DetectExtension(System.Text.Encoding.UTF8.GetBytes("<html>image.jpg</html>")));
    }

    [Fact]
    public void Recognizes_supported_file_signatures()
    {
        Assert.Equal(".png", ImageStorage.DetectExtension([137,80,78,71,13,10,26,10]));
        Assert.Equal(".jpg", ImageStorage.DetectExtension([255,216,255]));
        Assert.Equal(".webp", ImageStorage.DetectExtension(System.Text.Encoding.ASCII.GetBytes("RIFF1234WEBP")));
    }
}
