using Microsoft.AspNetCore.Authorization;

namespace PziApi.CrossCutting.Auth;

public record PziPermissionRequirement(string Permission) : IAuthorizationRequirement;
