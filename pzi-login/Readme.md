# Login app

## Auth0 configuration

The login service now delegates authentication to Auth0 using the authorization code + PKCE flow and returns Auth0-issued tokens to the SPA via the `callback` query string. Configure the following environment variables / settings:

- `Auth0__Domain` – Auth0 tenant domain (`*.auth0.com`).
- `Auth0__ClientId` – Client ID of the Auth0 application registered for this service.
- `Auth0__ClientSecret` – Client secret for the Auth0 application (required for confidential client flows and refresh tokens).
- `Auth0__Audience` – API identifier used when requesting access tokens for `pzi-api`.
- `Auth0__Organization` – Optional Auth0 organisation identifier to preselect tenant-specific logins.
- `Auth0__AdditionalScopes__[index]` – Optional additional scopes requested during login.

The `/authenticate/login` endpoint accepts the following query parameters:

- `callback` (required) – URL that receives the tokens after successful login.
- `returnUrl` (optional) – Value forwarded back to the SPA for post-login navigation.
- `organization` (optional) – Overrides the Auth0 organisation for the current login attempt.

The `/authenticate/refresh-token` endpoint exchanges a refresh token for a new access/ID token pair directly against Auth0.
