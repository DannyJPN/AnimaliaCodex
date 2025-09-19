using System.Collections.Generic;
using System.Security.Claims;

namespace PziApi.CrossCutting.Auth;

public static class ClaimsPrincipalExtensions
{
  private static readonly string[] DefaultPermissionClaims = new[]
  {
    "permissions",
    "https://schemas.auth0.com/permissions"
  };

  private static readonly string[] DefaultRoleClaims = new[]
  {
    "https://schemas.auth0.com/roles",
    "roles",
    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
  };

  public static IEnumerable<string> GetAuth0Permissions(this ClaimsPrincipal principal, Auth0Options options)
  {
    var claimTypes = options.PermissionClaims?.Length > 0
      ? options.PermissionClaims
      : DefaultPermissionClaims;

    foreach (var claimType in claimTypes)
    {
      foreach (var claim in principal.FindAll(claimType))
      {
        if (!string.IsNullOrWhiteSpace(claim.Value))
        {
          yield return claim.Value;
        }
      }
    }
  }

  public static IEnumerable<string> GetAuth0Roles(this ClaimsPrincipal principal, Auth0Options options)
  {
    var claimTypes = options.RoleClaims?.Length > 0
      ? options.RoleClaims
      : DefaultRoleClaims;

    foreach (var claimType in claimTypes)
    {
      foreach (var claim in principal.FindAll(claimType))
      {
        if (!string.IsNullOrWhiteSpace(claim.Value))
        {
          yield return claim.Value;
        }
      }
    }
  }

  public static string? GetAuth0TenantId(this ClaimsPrincipal principal, Auth0Options options)
  {
    IEnumerable<string?> candidateTypes = new[]
    {
      options.TenantClaim,
      "org_id",
      "https://schemas.auth0.com/org_id"
    };

    foreach (var claimType in candidateTypes)
    {
      if (string.IsNullOrWhiteSpace(claimType))
      {
        continue;
      }

      var value = principal.FindFirst(claimType)?.Value;

      if (!string.IsNullOrWhiteSpace(value))
      {
        return value;
      }
    }

    return null;
  }
}
