using System;
using System.Collections.Generic;
using System.Linq;
using PziApi.CrossCutting.Permissions;
using PziApi.CrossCutting.Settings;

namespace PziApi.CrossCutting.Auth;

public static class Auth0PermissionMapper
{
  private static readonly IReadOnlyDictionary<string, string[]> PermissionMap =
    new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
      ["records:read"] = new[]
      {
        UserPermissions.RecordsView
      },
      ["records:write"] = new[]
      {
        UserPermissions.RecordsView,
        UserPermissions.RecordsEdit
      },
      ["lists:read"] = new[]
      {
        UserPermissions.ListsView
      },
      ["lists:write"] = new[]
      {
        UserPermissions.ListsView,
        UserPermissions.ListsEdit
      },
      ["documentation:manage"] = new[]
      {
        UserPermissions.DocumentationDepartment,
        UserPermissions.JournalContribute,
        UserPermissions.JournalAccess
      },
      ["journal:read"] = new[]
      {
        UserPermissions.JournalRead,
        UserPermissions.JournalAccess
      },
      ["journal:write"] = new[]
      {
        UserPermissions.JournalRead,
        UserPermissions.JournalContribute,
        UserPermissions.JournalAccess
      }
    };

  private static readonly string[] AllPermissions = new[]
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

  public static HashSet<string> FromClaims(IEnumerable<string> claims)
  {
    var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var claim in claims)
    {
      if (PermissionMap.TryGetValue(claim, out var mapped))
      {
        resolved.UnionWith(mapped);
      }
    }

    return resolved;
  }

  public static HashSet<string> FromLegacyRoles(IEnumerable<string> roles, PermissionOptions options)
  {
    var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    if (options.GrantAllPermissions)
    {
      resolved.UnionWith(AllPermissions);
      return resolved;
    }

    var roleSet = new HashSet<string>(roles, StringComparer.OrdinalIgnoreCase);

    if (options.RecordsRead.Any(roleSet.Contains))
    {
      resolved.Add(UserPermissions.RecordsView);
    }

    if (options.RecordsEdit.Any(roleSet.Contains))
    {
      resolved.Add(UserPermissions.RecordsEdit);
    }

    if (options.ListsView.Any(roleSet.Contains))
    {
      resolved.Add(UserPermissions.ListsView);
    }

    if (options.ListsEdit.Any(roleSet.Contains))
    {
      resolved.Add(UserPermissions.ListsEdit);
    }

    if (options.DocumentationDepartment.Any(roleSet.Contains))
    {
      resolved.Add(UserPermissions.DocumentationDepartment);
    }

    if (options.JournalRead.Any(roleSet.Contains))
    {
      resolved.Add(UserPermissions.JournalRead);
    }

    return resolved;
  }

  public static IEnumerable<string> GetAllPermissions() => AllPermissions;
}
