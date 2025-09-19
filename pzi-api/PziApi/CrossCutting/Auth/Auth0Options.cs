namespace PziApi.CrossCutting.Auth;

public class Auth0Options
{
  public const string SectionName = "Auth0";

  public string Domain { get; set; } = string.Empty;

  public string Audience { get; set; } = string.Empty;

  public string? TenantClaim { get; set; }
    = "org_id";

  public string[] PermissionClaims { get; set; } = new[]
  {
    "permissions",
    "https://schemas.auth0.com/permissions"
  };

  public string[] RoleClaims { get; set; } = new[]
  {
    "https://schemas.auth0.com/roles",
    "roles",
    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
  };

  public string? SwaggerClientId { get; set; }
    = null;
}
