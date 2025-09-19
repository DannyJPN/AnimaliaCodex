using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using PziApi.CrossCutting.Settings;

namespace PziApi.CrossCutting.Auth;

public class Auth0PermissionHandler : AuthorizationHandler<PziPermissionRequirement>
{
  private readonly Auth0Options _auth0Options;
  private readonly PermissionOptions _permissionOptions;

  public Auth0PermissionHandler(
    IOptions<Auth0Options> auth0Options,
    IOptions<PermissionOptions> permissionOptions)
  {
    _auth0Options = auth0Options.Value;
    _permissionOptions = permissionOptions.Value;
  }

  protected override Task HandleRequirementAsync(
    AuthorizationHandlerContext context,
    PziPermissionRequirement requirement)
  {
    if (context.User?.Identity is not { IsAuthenticated: true })
    {
      return Task.CompletedTask;
    }

    var permissions = Auth0PermissionMapper.FromClaims(context.User.GetAuth0Permissions(_auth0Options));

    if (permissions.Contains(requirement.Permission))
    {
      context.Succeed(requirement);
      return Task.CompletedTask;
    }

    var fallbackPermissions = Auth0PermissionMapper.FromLegacyRoles(
      context.User.GetAuth0Roles(_auth0Options),
      _permissionOptions);

    if (fallbackPermissions.Contains(requirement.Permission))
    {
      context.Succeed(requirement);
    }

    return Task.CompletedTask;
  }
}
