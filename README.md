# BlogManagement-DotNet-API

A complete ASP.NET Core Web API for managing blog content, approval workflows, users, employees, roles, permissions, and audit logs. The solution follows a Clean Architecture-style separation between API, application contracts, domain entities, and infrastructure.

## Features

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
