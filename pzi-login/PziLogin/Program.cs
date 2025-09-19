using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using PziLogin.Auth;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, configuration) =>
{
  configuration.MinimumLevel.Information();
  configuration.WriteTo.Console();
});

builder.Services.Configure<Auth0Options>(builder.Configuration.GetSection(Auth0Options.SectionName));

builder.Services.AddHttpClient("Auth0", (provider, client) =>
{
  var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Auth0Options>>().Value;
  if (!string.IsNullOrWhiteSpace(options.Domain))
  {
    client.BaseAddress = new Uri($"https://{options.Domain}/");
  }
});

var auth0Options = builder.Configuration.GetSection(Auth0Options.SectionName).Get<Auth0Options>() ?? new Auth0Options();

builder.Services.AddAuthentication(options =>
{
  options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
  options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
  options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
  options.SlidingExpiration = true;
})
.AddOpenIdConnect(options =>
{
  options.Authority = string.IsNullOrWhiteSpace(auth0Options.Domain)
    ? null
    : $"https://{auth0Options.Domain}";
  options.ClientId = auth0Options.ClientId;
  options.ClientSecret = auth0Options.ClientSecret;
  options.ResponseType = "code";
  options.UsePkce = true;
  options.SaveTokens = true;
  options.CallbackPath = "/authenticate/callback";
  options.Scope.Clear();
  options.Scope.Add("openid");
  options.Scope.Add("profile");
  options.Scope.Add("email");
  options.Scope.Add("offline_access");
  if (auth0Options.AdditionalScopes?.Length > 0)
  {
    foreach (var scope in auth0Options.AdditionalScopes.Where(s => !string.IsNullOrWhiteSpace(s)))
    {
      options.Scope.Add(scope);
    }
  }

  options.TokenValidationParameters = new TokenValidationParameters
  {
    NameClaimType = "name",
    RoleClaimType = "https://schemas.auth0.com/roles"
  };

  options.Events = new OpenIdConnectEvents
  {
    OnRedirectToIdentityProvider = context =>
    {
      var requestOptions = context.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<Auth0Options>>().Value;
      var audience = context.Properties?.GetString("audience") ?? requestOptions.Audience;
      if (!string.IsNullOrWhiteSpace(audience))
      {
        context.ProtocolMessage.SetParameter("audience", audience);
      }

      var organization = context.Properties?.GetString("organization") ?? requestOptions.Organization;
      if (!string.IsNullOrWhiteSpace(organization))
      {
        context.ProtocolMessage.SetParameter("organization", organization);
      }

      return Task.CompletedTask;
    }
  };
});

builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseDeveloperExceptionPage();

if (!app.Environment.IsDevelopment())
{
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Authenticate}/{action=Login}/{id?}");

app.Run();
