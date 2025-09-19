using System;

namespace PziLogin.Auth;

public class Auth0Options
{
  public const string SectionName = "Auth0";

  public string Domain { get; set; } = string.Empty;

  public string ClientId { get; set; } = string.Empty;

  public string ClientSecret { get; set; } = string.Empty;

  public string Audience { get; set; } = string.Empty;

  public string? Organization { get; set; }
    = null;

  public string[] AdditionalScopes { get; set; } = Array.Empty<string>();
}
