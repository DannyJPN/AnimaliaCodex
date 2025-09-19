# Auth0 Migration – Design & Configuration

This document captures the design decisions, new configuration artefacts and Auth0 entities required to migrate the PZI platform from the existing AD/JWT/api-key model to Auth0-based authentication, authorization and tenant management.

## Inventory of existing applications

| Component       | Type                | Current URLs / identifiers                               | Auth0 entity                    |
|-----------------|---------------------|-----------------------------------------------------------|---------------------------------|
| `pzi-api`       | ASP.NET Core REST + OData API | Base URL: `https://metazoa-t.api.zoopraha.cz` (Swagger config) | **Auth0 API** (`AUTH0_API_IDENTIFIER`) – audience for access tokens |
| `pzi-login`     | ASP.NET Core MVC login proxy  | Redirect target for SPA via `SSO_LOGIN_URL` / `/authenticate/*` | **Machine-to-machine Application** if retained as OIDC proxy |
| `pzi-webapp`    | React Router SPA           | Base URL: `https://metazoa-t.zoopraha.cz` (see `pziConfig`)      | **Single Page Application** (Auth0 SPA / PKCE client) |
| Data import tools | .NET console apps? (`pzi-data-import`) | Internal CLI – uses same API key flow                         | Either use Auth0 Client Credentials or call API via SPA user tokens |

> **Note** The precise public URLs are derived from deployment manifests; adjust callback/logout URLs accordingly in each environment (dev/test/prod).

## Auth0 tenant structure

- **Primary tenant**: `zoopraha-metazoa` (placeholder) hosts all apps.
- **Organisations / tenants**: leverage Auth0 [Organisations](https://auth0.com/docs/manage-users/organizations) to model end customers/zoos. Each legacy tenant maps 1:1 to an Auth0 Organisation.
- **Connections**: Enable `Username-Password-Authentication` for local accounts and configure enterprise connections (Azure AD, SAML, Google Workspace) per organisation.
- **SSO**: Applications share the same Auth0 tenant and connections to obtain SSO automatically. Enable refresh token rotation for SPA to support silent renewal.

## Auth0 applications & APIs

| Auth0 entity | Purpose | Key settings |
|--------------|---------|--------------|
| **API** `pzi-api` | Issues access tokens for backend | Identifier `https://api.metazoa.cz` (placeholder), enable RBAC, add permissions matching legacy actions. |
| **SPA** `pzi-webapp` | Browser application | Type SPA (PKCE), callback `https://metazoa-t.zoopraha.cz/auth/login-callback`, logout `https://metazoa-t.zoopraha.cz/auth/logout`, allowed origins `https://metazoa-t.zoopraha.cz`. |
| **M2M / Native** `pzi-data-import` (optional) | CLI / services | Client credentials grant, scopes per import requirements. |
| **Regular Web App** `pzi-login` (optional) | Only if login proxy remains | Callback `https://metazoa-t-login.zoopraha.cz/signin-auth0`, logout `https://metazoa-t-login.zoopraha.cz/signout-callback`. |

## Secrets and configuration keys

All projects read configuration from `appsettings` / environment variables. Replace AD/api-key secrets with Auth0 values:

| Setting | Description | Location |
|---------|-------------|----------|
| `Auth0:Domain` | Auth0 domain, e.g. `zoopraha-metazoa.eu.auth0.com` | `pzi-api`, `pzi-login`, `pzi-webapp` env/appsettings |
| `Auth0:Audience` | Identifier of the Auth0 API | `pzi-api` (JWT validation), `pzi-webapp` (token requests) |
| `Auth0:ClientId` | SPA client ID | `pzi-webapp` config (`pziConfig`, `.env`) |
| `Auth0:ClientSecret` | Used only by confidential apps (login proxy, import tools) | `pzi-login` if retained, CLI apps |
| `Auth0:Organization` | Default organisation/tenant identifier (optional) | `pzi-webapp`, `pzi-login` |
| `Auth0:LogoutUrl` | Derived from SPA URL | `pzi-webapp`, `pzi-login` |
| `Auth0:ManagementToken` | (Optional) For migration scripts | CI/CD secrets |

Remove obsolete keys: `Pzi:ApiKeys`, `Pzi:TokenSecret`, `Pzi:TokenIssuer`, `Pzi:TokenAudience`, `Pzi:Ad*`.

## RBAC mapping

Auth0 API permissions reflect legacy `PermissionOptions` groups:

| Legacy permission | Description | Auth0 permission | Role candidates |
|-------------------|-------------|------------------|-----------------|
| `RecordsRead` | View specimen records | `records:read` | `Records Reader`, `Records Editor` |
| `RecordsEdit` | Edit specimen records | `records:write` | `Records Editor` |
| `ListsView` | View static lists | `lists:read` | `Lists Viewer`, `Administrator` |
| `ListsEdit` | Modify static lists | `lists:write` | `Lists Manager` |
| `DocumentationDepartment` | Documentation department features | `documentation:manage` | `Documentation` |
| `JournalRead` | Access journal module | `journal:read` | `Journal Reader`, `Journal Editor` |

- Enable **Auth0 RBAC** on the API and assign permissions to roles.
- Create Auth0 roles that mirror AD security groups to simplify migration (e.g. `SG_zoopraha_Metazoa_Evidence_RO` → `Records Reader`).
- Add custom claims via Auth0 Actions/Rules to emit both `permissions` and a `tenant_id`/`org_id` claim for backend enforcement.

## Multi-tenancy

- Each Auth0 Organisation corresponds to a tenant in the application database. Include the Auth0 `org_id` (or custom claim `tenant_id`) inside the access token.
- Backend enforces tenant isolation by storing `Auth0UserId` (`sub`) and `TenantId` for each user row.
- SPA resolves tenant context by either requesting login through a tenant-specific organisation invitation link or deriving from user selection; persist the resulting organisation in the session and attach it to API calls.

## Migration & operations overview

1. **User import** – export `Users`/`UserRoles` tables, transform into Auth0 bulk import JSON (email, name, temporary password, assigned Auth0 role).
2. **Just-in-time link** – when a user signs in via Auth0 for the first time, persist `sub` and `org_id` to the local user table.
3. **Secrets management** – store Auth0 secrets in the existing secret stores (user-secrets for dev, Kubernetes secrets / Azure Key Vault for prod). Update CI/CD pipelines to inject the new environment variables.
4. **Monitoring** – extend Serilog enrichers and frontend logging to capture `sub`, `org_id`, and roles from validated tokens for auditing.

## Next steps

- Update backend API to validate Auth0 JWT bearer tokens and replace API-key middleware (Step 2).
- Refactor login service (`pzi-login`) to become an Auth0 OIDC proxy or decommission it (Step 3).
- Retrofit SPA and other clients to request and pass Auth0 access tokens (subsequent steps).

