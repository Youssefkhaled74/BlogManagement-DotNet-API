Blog Management System - Backend (Foundation)

This repository contains the foundation for a Blog Management System backend built with ASP.NET Core, Entity Framework Core, and SQL Server using a Clean Architecture approach.

Projects created:
- src/BlogManagement.Api
- src/BlogManagement.Application
- src/BlogManagement.Domain
- src/BlogManagement.Infrastructure
- src/BlogManagement.Shared
- tests/BlogManagement.UnitTests
- tests/BlogManagement.IntegrationTests

Setup
1. Ensure .NET 10 SDK is installed (dotnet --info).
2. Update src/BlogManagement.Api/appsettings.json connection string for SQL Server or set the environment variable ConnectionStrings__DefaultConnection.
3. From the solution directory run:
   dotnet restore
   dotnet build

Migrations
- Navigate to src/BlogManagement.Infrastructure and run:
  dotnet ef migrations add InitialCreate -p src/BlogManagement.Infrastructure -s src/BlogManagement.Api --output-dir Migrations
- Apply migrations with:
  dotnet ef database update -p src/BlogManagement.Infrastructure -s src/BlogManagement.Api

Assumptions
- SRS file was not present in the repo; the domain model was created from the user's SRS summary in the request.
- Authentication and authorization are out of scope for Phase 1.

What’s next
- Implement services, repositories, and business rules in Application and Infrastructure.
- Add authentication, role & permission enforcement, and admin endpoints.
