using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PziApi.CrossCutting;
using PziApi.CrossCutting.Database;
using PziApi.CrossCutting.Permissions;
using PziApi.CrossCutting.Settings;
using PziApi.Models;
using System.Security.Claims;

namespace PziApi.Users.Endpoints;

public class UserLoggedIn
{
  public static async Task<Results<Ok<CommonDtos.SuccessResult<Dtos.UserSettingsModel>>, BadRequest>> Handle([FromBody] Dtos.UserLoggedInRequest viewModel, PziDbContext dbContext, IOptions<PermissionOptions> permissionOptions, ClaimsPrincipal claimsPrincipal)
  {
    var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == viewModel.UserName);

    if (user == null)
    {
      user = new Models.User()
      {
        UserName = viewModel.UserName,
        TaxonomySearchByLat = true,
        TaxonomySearchByCz = false
      };

      dbContext.Users.Add(user);

      await dbContext.SaveChangesAsync();
    }

    var validatedRoles = ExtractRolesFromClaims(claimsPrincipal);
    var validatedRolesArray = validatedRoles.ToArray();

    var existingRoles = await dbContext.UserRoles
        .Where(r => r.User!.UserName == viewModel.UserName)
        .ToArrayAsync();

    var newRoles = validatedRolesArray
      .Where(r => !existingRoles.Any(ur => string.Equals(ur.RoleName, r, StringComparison.OrdinalIgnoreCase)))
      .Select(r => new UserRole()
      {
        RoleName = r,
        UserId = user.Id
      })
      .ToArray();

    var rolesToDrop = existingRoles
      .Where(ur => !validatedRoles.Contains(ur.RoleName))
      .ToArray();

    dbContext.UserRoles.RemoveRange(rolesToDrop);
    dbContext.UserRoles.AddRange(newRoles);

    await dbContext.SaveChangesAsync();

    string visibleStatuses = string.IsNullOrEmpty(user.VisibleTaxonomyStatuses)
      ? "A,D,N,Z"!
      : user.VisibleTaxonomyStatuses;

    var hasJournalEditRoleInOrgLevels = await dbContext.OrganizationLevels.AnyAsync(ol =>
          validatedRolesArray.Contains(ol.JournalApproversGroup!) ||
          validatedRolesArray.Contains(ol.JournalContributorGroup!));

    var hasJournalReadRoleInOrgLevels = await dbContext.OrganizationLevels.AnyAsync(ol => validatedRolesArray.Contains(ol.JournalReadGroup!));

    var permissionClaims = ExtractPermissionsFromClaims(claimsPrincipal);

    var computedPermissions = DetermineUserPermissions(
      userName: viewModel.UserName,
      roles: validatedRolesArray,
      options: permissionOptions.Value,
      hasJournalReadRoleInOrgLevels: hasJournalReadRoleInOrgLevels,
      hasJournalEditRoleInOrgLevels: hasJournalEditRoleInOrgLevels);

    var userPermissions = computedPermissions
      .Concat(permissionClaims)
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToArray();

    return TypedResults.Ok(
      CommonDtos.SuccessResult<Dtos.UserSettingsModel>.FromItemAndFluentValidation(
        new Dtos.UserSettingsModel(
          user.Id,
          viewModel.UserName,
          visibleStatuses.Split(","),
          user.TaxonomySearchByCz,
          user.TaxonomySearchByLat,
          userPermissions
        ),
        new ValidationResult()
      )
    );
  }

  private static string[] DetermineUserPermissions(string userName, string[] roles, PermissionOptions options,
  bool hasJournalReadRoleInOrgLevels, bool hasJournalEditRoleInOrgLevels)
  {
    if (options.GrantAllPermissions)
    {
      return new[]
      {
        UserPermissions.RecordsView,
        UserPermissions.RecordsEdit,
        UserPermissions.ListsView,
        UserPermissions.ListsEdit,
        UserPermissions.DocumentationDepartment,
        UserPermissions.JournalRead,
        UserPermissions.JournalContribute,
        UserPermissions.JournalAccess
      };
    }

    List<string> permissions = new List<string>();

    if (roles.Any(role => options.RecordsRead.Contains(role)))
      permissions.Add(UserPermissions.RecordsView);

    if (roles.Any(role => options.RecordsEdit.Contains(role)))
      permissions.Add(UserPermissions.RecordsEdit);

    if (roles.Any(role => options.ListsView.Contains(role)))
      permissions.Add(UserPermissions.ListsView);

    if (roles.Any(role => options.ListsEdit.Contains(role)))
      permissions.Add(UserPermissions.ListsEdit);

    var hasDocumentationAccess = roles.Any(role => options.DocumentationDepartment.Contains(role));

    if (hasDocumentationAccess)
      permissions.Add(UserPermissions.DocumentationDepartment);

    var hasJournalRead = roles.Any(role => options.JournalRead.Contains(role)) || hasJournalReadRoleInOrgLevels;
    if (hasJournalRead)
    {
      permissions.Add(UserPermissions.JournalRead);
    }

    if (hasJournalEditRoleInOrgLevels || hasDocumentationAccess)
    {
      permissions.Add(UserPermissions.JournalContribute);
    }

    if (hasJournalEditRoleInOrgLevels || hasDocumentationAccess || hasJournalRead)
    {
      permissions.Add(UserPermissions.JournalAccess);
    }

    return permissions.ToArray();
  }

  private static HashSet<string> ExtractRolesFromClaims(ClaimsPrincipal claimsPrincipal)
  {
    if (claimsPrincipal == null)
    {
      return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    return claimsPrincipal
      .FindAll(c => IsRoleClaimType(c.Type))
      .Select(c => c.Value)
      .Where(value => !string.IsNullOrWhiteSpace(value))
      .ToHashSet(StringComparer.OrdinalIgnoreCase);
  }

  private static bool IsRoleClaimType(string claimType)
  {
    return claimType == ClaimTypes.Role
      || string.Equals(claimType, "role", StringComparison.OrdinalIgnoreCase)
      || string.Equals(claimType, "roles", StringComparison.OrdinalIgnoreCase)
      || claimType == ClaimTypes.GroupSid;
  }

  private static HashSet<string> ExtractPermissionsFromClaims(ClaimsPrincipal claimsPrincipal)
  {
    if (claimsPrincipal == null)
    {
      return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    return claimsPrincipal
      .FindAll(c => IsPermissionClaimType(c.Type))
      .Select(c => c.Value)
      .Where(value => !string.IsNullOrWhiteSpace(value))
      .ToHashSet(StringComparer.OrdinalIgnoreCase);
  }

  private static bool IsPermissionClaimType(string claimType)
  {
    return string.Equals(claimType, "permission", StringComparison.OrdinalIgnoreCase)
      || string.Equals(claimType, "permissions", StringComparison.OrdinalIgnoreCase)
      || string.Equals(claimType, "pzi:permission", StringComparison.OrdinalIgnoreCase);
  }
}
