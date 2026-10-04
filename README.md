# BlogManagement-DotNet-API

A complete ASP.NET Core Web API for managing blog content, approval workflows, users, employees, roles, permissions, and audit logs. The solution follows a Clean Architecture-style separation between API, application contracts, domain entities, and infrastructure.

## Features

For a local functional check with the React development server and API running, execute `node scripts/verify-system.mjs`. It checks role permissions and assignment, CRUD, article approval/publishing, authentication, and bilingual errors. It uses the demo administrator, creates temporary records, and removes them afterward. Cleanup requires `sqlcmd` and the local `BlogManagementDb` database. Set `TEST_BASE_URL`, `TEST_ADMIN_EMAIL`, and `TEST_ADMIN_PASSWORD` to override the defaults.

- JWT access tokens and rotating refresh tokens
- Registration, login, refresh, logout, and current-user endpoints
- Role-based access control backed by granular permissions
- Default `Admin`, `Editor`, and `Author` roles
- User status and role management
- Category CRUD
- Blog CRUD with ownership rules and pagination
- Workflow: `Draft -> PendingApproval -> Approved -> Published`
- Blog rejection, resubmission, and unpublishing
- Approval history
- Employee CRUD
- Audit logs for all write operations
- Public read-only endpoints for published blogs
- Swagger/OpenAPI with Bearer authentication
- Health endpoint

## Architecture

```text
src/
  BlogManagement.Api             HTTP controllers, authentication and middleware
  BlogManagement.Application     DTOs, interfaces and permission constants
  BlogManagement.Domain          Entities and domain enums
  BlogManagement.Infrastructure  EF Core, SQL Server and service implementations
  BlogManagement.Shared          Shared building blocks
tests/
  BlogManagement.UnitTests
  BlogManagement.IntegrationTests
```

Use `BlogManagement.slnx` as the main solution.

## Requirements

- .NET 10 SDK
- SQL Server or SQL Server Express/LocalDB

## Configuration

Never commit a real JWT key. Configure secrets with environment variables or .NET User Secrets:

```powershell
dotnet user-secrets init --project src/BlogManagement.Api
dotnet user-secrets set "Jwt:Key" "YOUR_RANDOM_SECRET_WITH_AT_LEAST_32_BYTES" --project src/BlogManagement.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_SQL_SERVER_CONNECTION" --project src/BlogManagement.Api
```

The checked-in `appsettings.json` contains development placeholders only.

`Database:EnsureCreatedOnStartup` is enabled for the first local run. Disable it and use EF Core migrations for shared or production databases.

## Run

```powershell
dotnet restore BlogManagement.slnx
dotnet build BlogManagement.slnx
dotnet run --project src/BlogManagement.Api
```

Swagger is available in Development at `/swagger`; health status is available at `/health`.

If startup stops at `EnsureCreatedAsync` with a SQL Server LocalDB connection error, check and start the configured instance from a terminal under the same Windows account as Visual Studio:

```powershell
SqlLocalDB info MSSQLLocalDB
SqlLocalDB start MSSQLLocalDB
dotnet run --project src/BlogManagement.Api
```

The configured SQL Server instance must be running before database initialization can complete.
If Visual Studio still cannot resolve the LocalDB instance, run `./scripts/Prepare-LocalDb.ps1` from a separate terminal, then restart the API. The script writes the current named-pipe connection to the ignored `src/BlogManagement.Api/appsettings.Local.json`, which is loaded only in Development. Refresh this file by rerunning the script after restarting LocalDB. Environment variables and command-line arguments still override the local file.
The API does not launch LocalDB subprocesses. Prepare the local connection from a terminal before debugging. The local file is copied to the build output and excluded from publication.
Visual Studio Debug builds run `Prepare-LocalDb.ps1` automatically to refresh the pipe before copying the settings. If LocalDB restarts while the API is already running, rerun the script; the API reloads the local connection for new request scopes.

## Development demo data

Set `Database:SeedDemoData` to `true` in `src/BlogManagement.Api/appsettings.Local.json` and restart the API. This option is enabled in the current local setup and runs only in Development. The demo seeder uses fixed IDs, preserves existing records/passwords and user edits, and does not duplicate rows on subsequent runs. Missing sample records are recreated while seeding remains enabled; set the option to `false` if you want deleted demo records to stay deleted.

The dataset includes 36 illustrated articles (16 published, 6 drafts, 5 pending, 5 approved, 4 rejected), 6 users with profile illustrations, 6 linked employees, a custom read-only support role, approval histories, and workflow audit entries. Default categories/permissions/roles are seeded separately.

| Email | Role | Status |
| --- | --- | --- |
| `admin.demo@example.test` | Admin | Active |
| `editor.demo@example.test` | Editor | Active |
| `sara.demo@example.test` | Author | Active |
| `omar.demo@example.test` | Author | Active |
| `support.demo@example.test` | Demo Support | Active |
| `inactive.demo@example.test` | Author | Inactive (login rejection test) |

Initial password for these local test accounts: `DemoPass123!`. Optionally set `Database:DemoPassword` before first seeding. Changing that setting does not reset existing account passwords. Sample illustrations live under `wwwroot/demo-images/`; uploaded user images remain separate.

## Article and profile images

- `POST /api/blogs/{id}/image`: multipart form-data with field `file`; needs `blogs.update` and article ownership or reviewer permission. Pending and published articles must return to an editable state first.
- `DELETE /api/blogs/{id}/image`: removes an editable article image.
- `POST /api/auth/profile/image`: multipart form-data with field `file`; changes the current user's profile image.
- `DELETE /api/auth/profile/image`: removes the current user's profile image.

PNG, JPEG, and WebP uploads are limited to 5 MB and checked by file signature. Original filenames are discarded. The API returns `imageUrl`, `authorImageUrl`, and `profileImageUrl`; images are publicly served from `/uploads/`. The React article editor and My account page include image selection, preview, replacement, and removal on save.

Files are stored under `src/BlogManagement.Api/wwwroot/uploads/` and excluded from Git. Preserve this directory across deployments and include it in backups; use a persistent volume when running in a container. Replaced or removed image files are deleted after saving the change. Existing image URLs change when an image is replaced.

With `Database:EnsureCreatedOnStartup=true`, startup adds the two nullable image columns to existing SQL Server databases without resetting data. For installations where startup schema changes are disabled, apply the following before deploying the new API:

```sql
IF COL_LENGTH('dbo.Users', 'ProfileImageUrl') IS NULL
    ALTER TABLE dbo.Users ADD ProfileImageUrl nvarchar(500) NULL;
IF COL_LENGTH('dbo.Blogs', 'ImageUrl') IS NULL
    ALTER TABLE dbo.Blogs ADD ImageUrl nvarchar(500) NULL;
```

## Database access and seed data

For the current local configuration, connect using Windows Authentication to `(localdb)\mssqllocaldb` and open `BlogManagementDb`.

In Visual Studio, open **View → SQL Server Object Explorer**, choose **Add SQL Server**, connect to that instance, and expand **Databases → BlogManagementDb → Tables**. Right-click `dbo.Categories` and choose **View Data**. SQL Server Management Studio can connect to the same server with Windows Authentication.

```sql
USE BlogManagementDb;
SELECT Id, Name, Description FROM dbo.Categories ORDER BY Name;
```

`src/BlogManagement.Infrastructure/Persistence/DataSeeder.cs` seeds default permissions, roles, and six categories: Technology, Software Development, Artificial Intelligence, Business, Education, and Lifestyle. Startup calls `SeedAsync` when `Database:EnsureCreatedOnStartup` is `true`. Existing categories are preserved; names are compared without case sensitivity so restarting the application does not duplicate the defaults.

To add another default category, add an entry to `DefaultCategories` and restart the API:

```csharp
["Travel"] = "Travel guides, destinations, and experiences."
```

The seeder only inserts missing categories. Editing or removing an entry does not update or delete an existing database row. Use the Categories page or the API for those operations.

## React frontend

The `frontend/` folder contains the React + Vite application for public reading and the management APIs. See [frontend/README.md](frontend/README.md) for setup and deployment.

```powershell
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173` with the API running. The development proxy targets the API launch profile at `https://localhost:65408`.

## First administrator

The first user registered in a new database receives the `Admin` role. Later registrations receive the `Author` role. Register the first account before exposing a new deployment publicly.

Role or permission changes take effect after the affected user logs in again or refreshes their access token.

## Default roles

### Admin

Receives every permission and can manage the whole system.

### Editor

Can manage categories, inspect content, review submissions, publish/unpublish posts, and view audit logs.

### Author

Can view categories, create and manage their own draft/rejected posts, and submit them for review.

Custom roles can be created and assigned through the role and user APIs.

## Main endpoints

### Authentication

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/auth/me`
- `POST /api/auth/change-password`
- `PUT /api/auth/profile`

### Public blog reading

- `GET /api/public/blogs`
- `GET /api/public/blogs/{id}`

### Content management

- `GET|POST /api/categories`
- `GET|PUT|DELETE /api/categories/{id}`
- `GET|POST /api/blogs`
- `GET|PUT|DELETE /api/blogs/{id}`
- `POST /api/blogs/{id}/submit`
- `POST /api/blogs/{id}/review`
- `POST /api/blogs/{id}/publish`
- `POST /api/blogs/{id}/unpublish`
- `GET /api/blogs/{id}/history`

### Administration

- `GET|PUT /api/users/{id}`
- `PATCH /api/users/{id}/status`
- `PUT /api/users/{id}/roles`
- `GET|POST /api/roles`
- `GET|PUT|DELETE /api/roles/{id}`
- `GET /api/roles/permissions`
- `PUT /api/roles/{id}/permissions`
- `GET|POST /api/employees`
- `GET|PUT|DELETE /api/employees/{id}`
- `GET /api/audit-logs`

List endpoints support pagination with `page` and `pageSize`. Blog, user, and employee lists support `search`.

## Authentication examples

Register the first administrator:

```json
{
  "username": "admin",
  "email": "admin@example.com",
  "password": "StrongPassword123!"
}
```

Refresh or revoke a token:

```json
{
  "refreshToken": "TOKEN_RETURNED_BY_LOGIN"
}
```

For protected endpoints send:

```http
Authorization: Bearer ACCESS_TOKEN
```
# Arabic and English

Use the language button in the React header to switch between English and Arabic. The selection is saved in the browser. Arabic uses right-to-left layouts, translated navigation, forms, permissions, statuses, image controls, and localized dates and numbers.

React sends `Accept-Language: ar` or `en` with API requests. The backend translates error and validation messages; API property names, permission codes, and enum values stay consistent for integrations. The default language is English. Article text, names, and other user-created content stay in their original language and accept Arabic or English.
