using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using PziLogin.Auth;
using PziLogin.Models;

namespace PziLogin.Controllers;

[AllowAnonymous]
[Route("authenticate")]
public class AuthenticateController : Controller
{
  private readonly Auth0Options _auth0Options;
  private readonly IHttpClientFactory _httpClientFactory;

  public AuthenticateController(IOptions<Auth0Options> auth0Options, IHttpClientFactory httpClientFactory)
  {
    _auth0Options = auth0Options.Value;
    _httpClientFactory = httpClientFactory;
  }

  [HttpGet("login")]
  public IActionResult Login(string callback, string? returnUrl = null, string? organization = null)
  {
    if (string.IsNullOrWhiteSpace(callback))
    {
      return BadRequest("Callback parameter is required.");
    }

    var properties = new AuthenticationProperties
    {
      RedirectUri = Url.Action(nameof(Callback))
    };

    properties.SetString("callback", callback);
    properties.SetString("returnUrl", returnUrl ?? string.Empty);

    if (!string.IsNullOrWhiteSpace(organization))
    {
      properties.SetString("organization", organization);
    }

    if (!string.IsNullOrWhiteSpace(_auth0Options.Audience))
    {
      properties.SetString("audience", _auth0Options.Audience);
    }

    return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
  }

  [HttpGet("callback")]
  public async Task<IActionResult> Callback()
  {
    var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    if (!result.Succeeded)
    {
      return Challenge(OpenIdConnectDefaults.AuthenticationScheme);
    }

    var callback = result.Properties?.GetString("callback");
    var returnUrl = result.Properties?.GetString("returnUrl");

    if (string.IsNullOrWhiteSpace(callback))
    {
      callback = "/";
    }

    var tokens = result.Properties?.GetTokens() ?? Enumerable.Empty<AuthenticationToken>();
    var tokenDictionary = tokens.ToDictionary(t => t.Name, t => t.Value, StringComparer.OrdinalIgnoreCase);

    tokenDictionary.TryGetValue("access_token", out var accessToken);
    tokenDictionary.TryGetValue("id_token", out var idToken);
    tokenDictionary.TryGetValue("refresh_token", out var refreshToken);
    tokenDictionary.TryGetValue("expires_at", out var expiresAtRaw);

    string? expiresIn = null;
    if (!string.IsNullOrWhiteSpace(expiresAtRaw)
      && DateTimeOffset.TryParse(expiresAtRaw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expiresAt))
    {
      var seconds = Math.Max(0, (expiresAt - DateTimeOffset.UtcNow).TotalSeconds);
      expiresIn = Convert.ToInt64(seconds).ToString(CultureInfo.InvariantCulture);
    }

    var queryParameters = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
    {
      ["access_token"] = accessToken,
      ["id_token"] = idToken,
      ["refresh_token"] = refreshToken,
      ["expires_in"] = expiresIn,
      ["token_type"] = string.IsNullOrWhiteSpace(accessToken) ? null : "Bearer",
      ["returnUrl"] = string.IsNullOrWhiteSpace(returnUrl) ? null : returnUrl
    };

    var redirectUrl = BuildRedirectUrl(callback, queryParameters);

    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    return Redirect(redirectUrl);
  }

  [HttpPost("refresh-token")]
  public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.RefreshToken))
    {
      return BadRequest("Refresh token is required.");
    }

    if (string.IsNullOrWhiteSpace(_auth0Options.Domain))
    {
      return StatusCode(500, "Auth0 domain is not configured.");
    }

    var client = _httpClientFactory.CreateClient("Auth0");

    var payload = new Dictionary<string, string>
    {
      ["grant_type"] = "refresh_token",
      ["refresh_token"] = request.RefreshToken,
      ["client_id"] = _auth0Options.ClientId
    };

    if (!string.IsNullOrWhiteSpace(_auth0Options.ClientSecret))
    {
      payload["client_secret"] = _auth0Options.ClientSecret;
    }

    if (!string.IsNullOrWhiteSpace(_auth0Options.Audience))
    {
      payload["audience"] = _auth0Options.Audience;
    }

    var tokenEndpoint = $"https://{_auth0Options.Domain}/oauth/token";
    var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(payload));
    var content = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
      return StatusCode((int)response.StatusCode, content);
    }

    return Content(content, "application/json");
  }

  private static string BuildRedirectUrl(string callback, IDictionary<string, string?> parameters)
  {
    var filtered = parameters
      .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
      .ToDictionary(kvp => kvp.Key, kvp => kvp.Value!, StringComparer.OrdinalIgnoreCase);

    return QueryHelpers.AddQueryString(callback, filtered);
  }
}
