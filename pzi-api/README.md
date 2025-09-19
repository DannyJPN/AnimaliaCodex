# PZI - API

## Overview

API project that powers PZI backend.

## Database connection in local environment

- The API now targets PostgreSQL. To configure the connection for local development, it is preferred to use `user-secrets` provided by .NET. They can be set from Visual Studio UI or from command line:
  - `dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=pzi;Username=pzi;Password=STRONG_PASSWORD"`
- Connection string values can also be supplied through environment variables (see `ConnectionStrings__Default`). When running inside Docker, point the host to the name of the PostgreSQL container service.

## Configuration values for container

- `Pzi__SwaggerEnabled` - specify `true` / `false` to enable / disable swagger for the interaction. When running in dev mode, swagger is always active.
- `Pzi__SwaggerApiHost` - optional parameter, when set override default API url shown in Swagger UI
- `Auth0__Domain` - Auth0 tenant domain (e.g. `zoopraha-metazoa.eu.auth0.com`)
- `Auth0__Audience` - Auth0 API identifier configured for `pzi-api`
- `Auth0__TenantClaim` - Claim name that contains tenant/organisation identifier (defaults to `org_id`)
- `Auth0__SwaggerClientId` (optional) - Auth0 application client id used for Swagger OAuth2 login
- `ConnectionStrings__Default` - Connection string to backing database
- Configuration sections for user permissions (legacy AD role compatibility / fallbacks)
  - `Pzi__Permissions__GrantAllPermissions` - When set to `true`, all users will have all permissions regardless of their claims
  - `Pzi__Permissions__RecordsRead__[index]` - Legacy role identifiers that grant RECORDS:VIEW permission
  - `Pzi__Permissions__RecordsEdit__[index]` - Legacy role identifiers that grant RECORDS:EDIT permission
  - `Pzi__Permissions__ListsView__[index]` - Legacy role identifiers that grant LISTS:VIEW permission
  - `Pzi__Permissions__ListsEdit__[index]` - Legacy role identifiers that grant LISTS:EDIT permission
  - `Pzi__Permissions__DocumentationDepartment__[index]` - Legacy role identifiers that grant DOCUMENTATION_DEPARTMENT permission
  - `Pzi__Permissions__JournalRead__[index]` - Legacy role identifiers that grant JOURNAL:READ permission

## Swagger

Application expose SwaggerUI, when running in dev mode or when configured with specific setting. Swagger is available on address `[host]:[port]/swagger` (by default on `http://localhost:5230`).

## CLI Commands

### Start local PostgreSQL database

`docker compose -f ../docker-compose.postgres.yml up -d`

### Build locally

`dotnet build`

### Running tests locally

`dotnet test`

### Start project locally from commandline

Executed from root folder of `pzi-api`

`dotnet run --project PziApi`

### Publishing local version

Following command will publish release version to `out` folder:

`dotnet publish -c Release -o out`
