using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PziApi.CrossCutting;
using PziApi.CrossCutting.Auth;
using PziApi.CrossCutting.Database;
using PziApi.CrossCutting.Permissions;
using PziApi.CrossCutting.Settings;
using PziApi.Models;

namespace PziApi.Users.Endpoints;

public class UserLoggedIn
{
  public static async Task<Results<Ok<CommonDtos.SuccessResult<Dtos.UserSettingsModel>>, BadRequest>> Handle(
    ClaimsPrincipal principal,
    [FromBody] Dtos.UserLoggedInRequest viewModel,
    PziDbContext dbContext,
    IOptions<Auth0Options> auth0OptionsAccessor,
    IOptions<PermissionOptions> permissionOptionsAccessor)
  {
    var auth0Options = auth0OptionsAccessor.Value;
    var permissionOptions = permissionOptionsAccessor.Value;

    var auth0UserId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
      ?? principal.FindFirstValue("sub");

    if (string.IsNullOrWhiteSpace(auth0UserId))
    {
      return TypedResults.BadRequest();
    }

    var resolvedUserName = string.IsNullOrWhiteSpace(viewModel.UserName)
      ? principal.FindFirstValue(ClaimTypes.Name)
        ?? principal.FindFirstValue("nickname")
        ?? principal.Identity?.Name
        ?? auth0UserId
      : viewModel.UserName;

    var tenantId = viewModel.TenantId ?? principal.GetAuth0TenantId(auth0Options);

    var resolvedPermissions = principal
      .GetAuth0Permissions(auth0Options)
      .Where(p => !string.IsNullOrWhiteSpace(p))
      .Select(p => p.Trim())
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToArray();

    var resolvedRoles = principal
      .GetAuth0Roles(auth0Options)
      .Where(r => !string.IsNullOrWhiteSpace(r))
      .Select(r => r.Trim())
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToArray();

    var user = await dbContext.Users
      .FirstOrDefaultAsync(u => u.Auth0UserId == auth0UserId)
      ?? await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == resolvedUserName);

    if (user == null)
    {
      user = new Models.User()
      {
        UserName = resolvedUserName,
        Auth0UserId = auth0UserId,
        TenantId = tenantId,
        TaxonomySearchByLat = true,
        TaxonomySearchByCz = false
      };

      dbContext.Users.Add(user);

      await dbContext.SaveChangesAsync();
    }
    else
    {
      var hasChanges = false;

      if (user.Auth0UserId != auth0UserId)
      {
        user.Auth0UserId = auth0UserId;
        hasChanges = true;
      }

      if (!string.IsNullOrWhiteSpace(tenantId) && user.TenantId != tenantId)
      {
        user.TenantId = tenantId;
        hasChanges = true;
      }

      if (!string.Equals(user.UserName, resolvedUserName, StringComparison.Ordinal))
      {
        user.UserName = resolvedUserName;
        hasChanges = true;
      }

      if (hasChanges)
      {
        await dbContext.SaveChangesAsync();
      }
    }

    var roleSet = new HashSet<string>(resolvedRoles, StringComparer.OrdinalIgnoreCase);

    var userRolesQuery = dbContext.UserRoles
      .Where(r => r.UserId == user.Id);

    if (tenantId == null)
    {
      userRolesQuery = userRolesQuery.Where(r => r.TenantId == null);
    }
    else
    {
      userRolesQuery = userRolesQuery.Where(r => r.TenantId == tenantId);
    }

    var existingRoles = await userRolesQuery.ToArrayAsync();
    var existingRoleNames = existingRoles
      .Select(r => r.RoleName)
      .ToHashSet(StringComparer.OrdinalIgnoreCase);

    var rolesToInsert = roleSet
      .Where(role => !existingRoleNames.Contains(role))
      .Select(role => new UserRole
      {
        RoleName = role,
        TenantId = tenantId,
        UserId = user.Id
      })
      .ToArray();

    var rolesToDrop = existingRoles
      .Where(ur => !roleSet.Contains(ur.RoleName))
      .ToArray();

    dbContext.UserRoles.RemoveRange(rolesToDrop);
    if (rolesToInsert.Length > 0)
    {
      await dbContext.UserRoles.AddRangeAsync(rolesToInsert);
    }

    if (rolesToInsert.Length > 0 || rolesToDrop.Length > 0)
    {
      await dbContext.SaveChangesAsync();
    }

    string visibleStatuses = string.IsNullOrEmpty(user.VisibleTaxonomyStatuses)
      ? "A,D,N,Z"!
      : user.VisibleTaxonomyStatuses;

    var hasJournalEditRoleInOrgLevels = await dbContext.OrganizationLevels.AnyAsync(ol =>
      (!string.IsNullOrWhiteSpace(ol.JournalApproversGroup) && roleSet.Contains(ol.JournalApproversGroup)) ||
      (!string.IsNullOrWhiteSpace(ol.JournalContributorGroup) && roleSet.Contains(ol.JournalContributorGroup)));

    var hasJournalReadRoleInOrgLevels = await dbContext.OrganizationLevels.AnyAsync(ol =>
      !string.IsNullOrWhiteSpace(ol.JournalReadGroup) && roleSet.Contains(ol.JournalReadGroup));

    var userPermissions = DetermineUserPermissions(
      permissionClaims: resolvedPermissions,
      roleClaims: roleSet,
      options: permissionOptions,
      hasJournalReadRoleInOrgLevels: hasJournalReadRoleInOrgLevels,
      hasJournalEditRoleInOrgLevels: hasJournalEditRoleInOrgLevels);

    return TypedResults.Ok(
      CommonDtos.SuccessResult<Dtos.UserSettingsModel>.FromItemAndFluentValidation(
        new Dtos.UserSettingsModel(
          user.Id,
          resolvedUserName,
          visibleStatuses.Split(","),
          user.TaxonomySearchByCz,
          user.TaxonomySearchByLat,
          userPermissions
        ),
        new ValidationResult()
      )
    );
  }

  private static string[] DetermineUserPermissions(
    IEnumerable<string> permissionClaims,
    IEnumerable<string> roleClaims,
    PermissionOptions options,
    bool hasJournalReadRoleInOrgLevels,
    bool hasJournalEditRoleInOrgLevels)
  {
    var resolved = Auth0PermissionMapper.FromClaims(permissionClaims);

    if (options.GrantAllPermissions)
    {
      resolved.UnionWith(Auth0PermissionMapper.GetAllPermissions());
    }
    else
    {
      resolved.UnionWith(Auth0PermissionMapper.FromLegacyRoles(roleClaims, options));
    }

    if (hasJournalReadRoleInOrgLevels)
    {
      resolved.Add(UserPermissions.JournalRead);
    }

    if (hasJournalEditRoleInOrgLevels)
    {
      resolved.Add(UserPermissions.JournalContribute);
      resolved.Add(UserPermissions.JournalAccess);
    }

    if (resolved.Contains(UserPermissions.DocumentationDepartment))
    {
      resolved.Add(UserPermissions.JournalContribute);
      resolved.Add(UserPermissions.JournalAccess);
    }

    if (resolved.Contains(UserPermissions.JournalRead) || resolved.Contains(UserPermissions.JournalContribute))
    {
      resolved.Add(UserPermissions.JournalAccess);
    }

    return resolved.ToArray();
  }

}
