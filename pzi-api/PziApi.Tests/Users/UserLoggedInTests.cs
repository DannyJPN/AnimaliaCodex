using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PziApi.CrossCutting;
using PziApi.CrossCutting.Database;
using PziApi.CrossCutting.Permissions;
using PziApi.CrossCutting.Settings;
using PziApi.Users;
using PziApi.Users.Endpoints;

namespace PziApi.Tests.Users;

public class UserLoggedInTests
{
  [Fact]
  public async Task Handle_IgnoresTamperedRolesFromRequestPayload()
  {
    var dbOptions = new DbContextOptionsBuilder<PziDbContext>()
      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
      .Options;

    await using var dbContext = new PziDbContext(dbOptions);

    var permissionOptions = Options.Create(new PermissionOptions
    {
      RecordsRead = new List<string> { "role-from-claims" },
      RecordsEdit = new List<string> { "role-from-payload" }
    });

    var request = new Dtos.UserLoggedInRequest(
      UserName: "test-user",
      Roles: new[] { "role-from-claims", "role-from-payload" }
    );

    var claimsPrincipal = new ClaimsPrincipal(
      new ClaimsIdentity(
        new[]
        {
          new Claim(ClaimTypes.Name, "test-user"),
          new Claim(ClaimTypes.Role, "role-from-claims")
        },
        authenticationType: "TestAuth"));

    var result = await UserLoggedIn.Handle(request, dbContext, permissionOptions, claimsPrincipal);

    var okResult = Assert.IsType<Ok<CommonDtos.SuccessResult<Dtos.UserSettingsModel>>>(result.Result);
    var userSettings = Assert.NotNull(okResult.Value.Item);

    Assert.Contains(UserPermissions.RecordsView, userSettings.Permissions);
    Assert.DoesNotContain(UserPermissions.RecordsEdit, userSettings.Permissions);

    var storedRoleNames = dbContext.UserRoles
      .Where(role => role.User!.UserName == "test-user")
      .Select(role => role.RoleName)
      .ToList();

    Assert.Equal(new[] { "role-from-claims" }, storedRoleNames);
  }
}

