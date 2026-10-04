using Microsoft.EntityFrameworkCore;

namespace BlogManagement.Infrastructure.Persistence;

public static class ImageSchemaUpgrade
{
    public static Task ApplyAsync(BlogManagementDbContext db, CancellationToken ct = default) =>
        db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.Users', 'ProfileImageUrl') IS NULL
                ALTER TABLE dbo.Users ADD ProfileImageUrl nvarchar(500) NULL;
            IF COL_LENGTH('dbo.Blogs', 'ImageUrl') IS NULL
                ALTER TABLE dbo.Blogs ADD ImageUrl nvarchar(500) NULL;
            """, ct);
}
