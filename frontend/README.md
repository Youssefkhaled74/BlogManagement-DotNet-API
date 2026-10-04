# Journal — React frontend

React + Vite frontend for the existing ASP.NET Core API. Includes public reading, registration/login/logout, rotating refresh tokens, profile/password management, articles and approval history, categories, employees, users/status/role assignments, roles/permissions, audit filters, pagination, and health checks. Navigation and actions respect API permissions and article ownership/workflow rules.

## Run locally

Requires Node.js 20.19+ or 22.12+ and npm (or pnpm).

1. Start the API from the repository root: `dotnet run --project src/BlogManagement.Api`.
2. In this folder run `npm install`, then `npm run dev`.
3. Open http://localhost:5173.

Vite proxies `/api` and `/health` to `https://localhost:65408`, matching the API launch profile. Its development proxy accepts the local ASP.NET certificate. If your backend uses a different address, copy `.env.example` to `.env` and set `API_PROXY_TARGET`.

Register an account through the sign-in dialog. The first account in a new database receives Admin; later accounts receive Author. The backend needs working SQL Server and JWT configuration as described in the root README.

## Production

Set `VITE_API_URL` to the backend origin without `/api` and run `npm run build`. Serve `dist` with a static web server. Allow the frontend origin in the backend's `Cors:AllowedOrigins`. Vite's development proxy is not part of the production build. `npm run preview` is a local build preview.

Tokens are scoped to the current browser tab in sessionStorage. Content is displayed as plain text, so user-supplied HTML is never executed. Role changes require refreshing the session or signing in again. New roles can be created without permissions; use the permission editor to grant access. Assigning user roles needs both `users.manage` and `roles.read`; article forms need `categories.read` to select a category.
