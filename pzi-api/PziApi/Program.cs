using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NSwag.Generation.Processors.Security;
using PziApi.CrossCutting.Auth;
using PziApi.CrossCutting.Database;
using Microsoft.AspNetCore.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using PziApi.TaxonomyClasses;
using PziApi.TaxonomyOrders;
using PziApi.Specimens;
using PziApi.TaxonomyFamilies;
using PziApi.TaxonomyGenera;
using PziApi.Species;
using PziApi.Users;
using PziApi.Movements;
using PziApi.UserTableSettings;
using PziApi.SpeciesRecords;
using PziApi.SpeciesDocuments;
using PziApi.SpecimensRecords;
using Serilog;
using PziApi.SpecimensMarkings;
using PziApi.SpecimensCadavers;
using PziApi.SpecimensDocuments;
using PziApi.PrintExports;
using PziApi.Search;
using PziApi.Contracts;
using PziApi.OrganizationLevels;
using PziApi.Partners;
using PziApi.CadaverPartners;
using PziApi.Locations;
using PziApi.SpecimenPlacements;
using PziApi.TaxonomyPhyla;
using PziApi.CrossCutting.OData;
using PziApi.JournalCommon;
using PziApi.CrossCutting.Settings;
using PziApi.ExpositionAreas;
using PziApi.ExpositionSets;
using PziApi.SpecimenImages;
using PziApi.CrossCutting.Filters;
using PziApi.ContractActionss;
using PziApi.JournalEntries;
using PziApi.BirthMethods;
using PziApi.Zoos;
using PziApi.Rearings;

internal class Program
{
  static IEdmModel GetEdmModel()
  {
    var builder = new ODataConventionModelBuilder();

    builder.EntitySet<PziApi.Models.CadaverPartner>("CadaverPartners");
    builder.EntitySet<PziApi.Models.Cadaver>("Cadavers");
    builder.EntitySet<PziApi.Models.ClassificationType>("ClassificationTypes");
    builder.EntitySet<PziApi.Models.ContractActionInitiator>("ContractActionInitiators");
    builder.EntitySet<PziApi.Models.ContractActionType>("ContractActionTypes");
    builder.EntitySet<PziApi.Models.ContractAction>("ContractActions");
    builder.EntitySet<PziApi.Models.ContractMovementReason>("ContractMovementReasons");
    builder.EntitySet<PziApi.Models.ContractType>("ContractTypes");
    builder.EntitySet<PziApi.Models.Contract>("Contracts");
    builder.EntitySet<PziApi.Models.DecrementReason>("DecrementReasons");
    builder.EntitySet<PziApi.Models.DocumentSpecies>("DocumentSpecies");
    builder.EntitySet<PziApi.Models.DocumentSpecimen>("DocumentSpecimens");
    builder.EntitySet<PziApi.Models.EuCode>("EuCodes");
    builder.EntitySet<PziApi.Models.GenderType>("GenderTypes");
    builder.EntitySet<PziApi.Models.IncrementReason>("IncrementReasons");
    builder.EntitySet<PziApi.Models.BirthMethod>("BirthMethods");
    builder.EntitySet<PziApi.Models.Rearing>("Rearings");
    builder.EntitySet<PziApi.Models.MarkingType>("MarkingTypes");
    builder.EntitySet<PziApi.Models.Marking>("Markings");
    builder.EntitySet<PziApi.Models.Movement>("Movements");
    builder.EntitySet<PziApi.Models.Partner>("Partners");
    builder.EntitySet<PziApi.Models.Placement>("Placements");
    builder.EntitySet<PziApi.Models.RdbCode>("RdbCodes");
    builder.EntitySet<PziApi.Models.RecordActionType>("RecordActionTypes");
    builder.EntitySet<PziApi.Models.RecordSpecimen>("RecordSpecimens");
    builder.EntitySet<PziApi.Models.RecordSpecies>("RecordSpecies");
    builder.EntitySet<PziApi.Models.Region>("Regions");
    builder.EntitySet<PziApi.Models.Section>("Sections");
    builder.EntitySet<PziApi.Models.Species>("Species");
    builder.EntitySet<PziApi.Models.SpeciesCiteTypes>("SpeciesCiteTypes");
    builder.EntitySet<PziApi.Models.SpeciesDocumentType>("SpeciesDocumentTypes");
    builder.EntitySet<PziApi.Models.SpeciesProtectionType>("SpeciesProtectionTypes");
    builder.EntitySet<PziApi.Models.SpecimenDocumentType>("SpecimenDocumentTypes");
    builder.EntitySet<PziApi.Models.Specimen>("Specimens");
    builder.EntitySet<PziApi.Models.TaxonomyClass>("TaxonomyClasses");
    builder.EntitySet<PziApi.Models.TaxonomyOrder>("TaxonomyOrders");
    builder.EntitySet<PziApi.Models.TaxonomyFamily>("TaxonomyFamilies");
    builder.EntitySet<PziApi.Models.TaxonomyGenus>("TaxonomyGenera");
    builder.EntitySet<PziApi.Models.TaxonomyPhylum>("TaxonomyPhyla");
    builder.EntitySet<PziApi.Models.ZooStatus>("ZooStatuses");
    builder.EntitySet<PziApi.Models.Zoo>("Zoos");
    builder.EntitySet<PziApi.Models.UserTableSetting>("UserTableSettings");
    builder.EntitySet<PziApi.Models.ColorType>("ColorTypes");
    builder.EntitySet<PziApi.Models.Location>("Locations");
    builder.EntitySet<PziApi.Models.OrganizationLevel>("OrganizationLevels");
    builder.EntitySet<PziApi.Models.SpecimenPlacement>("SpecimenPlacements");
    builder.EntitySet<PziApi.Models.Journal.JournalActionType>("JournalActionTypes");
    builder.EntitySet<PziApi.Models.ExpositionArea>("ExpositionAreas");
    builder.EntitySet<PziApi.Models.ExpositionSet>("ExpositionSets");
    builder.EntitySet<PziApi.Models.UserFlaggedDistrict>("UserFlaggedDistricts");
    builder.EntitySet<PziApi.Models.UserFlaggedSpecies>("UserFlaggedSpecies");
    builder.EntitySet<PziApi.Models.SpecimenImage>("SpecimenImages");
    builder.EntitySet<PziApi.Models.OriginType>("OriginTypes");

    builder.EntityType<PziApi.Models.OrganizationLevel>()
        .Collection
        .Function("ForUser")
        .ReturnsCollectionFromEntitySet<PziApi.Models.OrganizationLevel>("OrganizationLevels");

    builder.EnableLowerCamelCase();

    return builder.GetEdmModel();
  }

  private static void RegisterEndpoints(WebApplication app)
  {
    app.RegisterTaxonomyPhylaEndpoints();
    app.RegisterTaxonomyClassesEndpoints();
    app.RegisterTaxonomyOrdersEndpoints();
    app.RegisterTaxonomyFamiliesEndpoints();
    app.RegisterTaxonomyGeneraEndpoints();
    app.RegisterSpecimensEndpoints();
    app.RegisterSpeciesEndpoints();
    app.RegisterUsersEndpoints();
    app.RegisterMovementsEndpoints();
    app.RegisterUserTableSettingsEndpoints();
    app.RegisterRecordSpeciesEndpoints();
    app.RegisterDocumentSpeciesEndpoints();
    app.RegisterSpecimensRecordsEndpoints();
    app.RegisterSpecimensMarkingsEndpoints();
    app.RegisterSpecimensCadaversEndpoints();
    app.RegisterSpecimensDocumentsEndpoints();
    app.RegisterPrintExportsEndpoints();
    app.RegisterPrintSearchEndpoints();
    app.RegisterContractsExportsEndpoints();
    app.RegisterOrganizationLevelsEndpoints();
    app.RegisterPartnersEndpoints();
    app.RegisterBirthMethodsEndpoints();
    app.RegisterZoosEndpoints();
    app.RegisterRearingsEndpoints();
    app.RegisterCadaverPartnersEndpoints();
    app.RegisterLocationsEndpoints();
    app.RegisterSpecimenPlacementsEndpoints();
    app.RegisterJournalCommonEndpoints();
    app.RegisterExpositionAreasEndpoints();
    app.RegisterExpositionSetsEndpoints();
    app.RegisterSpecimenImagesEndpoints();
    app.RegisterContractActionsExportsEndpoints();
    app.RegisterJournaEntriesEndpoints();
  }

  private static void Main(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, configuration) =>
    {
      configuration.MinimumLevel.Information();
      configuration.WriteTo.Console();
    });

    builder.Services.AddDbContext<PziDbContext>((provider, options) =>
    {
      options.UseNpgsql(builder.Configuration.GetConnectionString("Default"), npgsqlOptionsAction: sqlOptions =>
      {
        sqlOptions.CommandTimeout(240);
      });
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.Configure<Auth0Options>(builder.Configuration.GetSection(Auth0Options.SectionName));
    builder.Services.Configure<PermissionOptions>(builder.Configuration.GetSection(PermissionOptions.SectionName));

    var auth0Options = builder.Configuration
      .GetSection(Auth0Options.SectionName)
      .Get<Auth0Options>() ?? new Auth0Options();

    builder.Services
      .AddAuthentication(options =>
      {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
      })
      .AddJwtBearer(options =>
      {
        if (!string.IsNullOrWhiteSpace(auth0Options.Domain))
        {
          options.Authority = $"https://{auth0Options.Domain}";
        }

        if (!string.IsNullOrWhiteSpace(auth0Options.Audience))
        {
          options.Audience = auth0Options.Audience;
        }

        var roleClaimType = auth0Options.RoleClaims.FirstOrDefault(rc => !string.IsNullOrWhiteSpace(rc))
          ?? ClaimTypes.Role;

        options.TokenValidationParameters = new TokenValidationParameters
        {
          NameClaimType = ClaimTypes.NameIdentifier,
          RoleClaimType = roleClaimType,
          ValidateAudience = !string.IsNullOrWhiteSpace(auth0Options.Audience),
          ValidAudience = string.IsNullOrWhiteSpace(auth0Options.Audience)
            ? null
            : auth0Options.Audience
        };
      });

    builder.Services.AddSingleton<IAuthorizationHandler, Auth0PermissionHandler>();

    builder.Services.AddAuthorization(options =>
    {
      var defaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .Build();

      options.DefaultPolicy = defaultPolicy;
      options.FallbackPolicy = defaultPolicy;

      foreach (var permission in Auth0PermissionMapper.GetAllPermissions())
      {
        options.AddPolicy(permission, policy =>
        {
          policy.Requirements.Add(new PziPermissionRequirement(permission));
        });
      }
    });

    builder.Services.AddOpenApiDocument(config =>
    {
      config.DocumentName = "PziAPI";
      config.Title = "PziApi v1";
      config.Version = "v1";

      config.AddSecurity("Bearer", Enumerable.Empty<string>(), new NSwag.OpenApiSecurityScheme
      {
        Type = NSwag.OpenApiSecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Auth0 JWT Bearer"
      });

      config.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
      config.OperationProcessors.Add(new ODataOperationProcessor());
      config.OperationProcessors.Add(new RemoveODataQueryOptionsProcessor());
    });

    builder.Services
      .AddControllers()
      .AddOData(options => options
        .Select()
        .Filter()
        .OrderBy()
        .SetMaxTop(1024)
        .Expand()
        .Count()
        .AddRouteComponents("odata", GetEdmModel()));

    var app = builder.Build();

    var swaggerEnabled = builder.Environment.IsDevelopment()
      || builder.Configuration.GetSection("Pzi:SwaggerEnabled").Get<bool>();
    var swaggerApiHost = builder.Configuration.GetSection("Pzi:SwaggerApiHost").Get<string>();

    if (swaggerEnabled)
    {
      app.UseOpenApi(options =>
      {
        if (!string.IsNullOrEmpty(swaggerApiHost))
        {
          options.PostProcess = (doc, httpRequest) =>
          {
            doc.Servers.Clear();

            doc.Servers.Add(new NSwag.OpenApiServer
            {
              Url = swaggerApiHost
            });
          };
        }
      });

      app.UseSwaggerUi(config =>
      {
        config.DocumentTitle = "PziAPI";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
        config.DocExpansion = "list";

        var swaggerClientId = builder.Configuration.GetValue<string>("Auth0:SwaggerClientId")
          ?? builder.Configuration.GetValue<string>("Auth0:ClientId");

        if (!string.IsNullOrWhiteSpace(swaggerClientId))
        {
          config.OAuth2Client = new NSwag.AspNetCore.OAuth2ClientSettings
          {
            ClientId = swaggerClientId,
            AppName = "PziAPI Swagger"
          };

          if (!string.IsNullOrWhiteSpace(auth0Options.Audience))
          {
            config.OAuth2Client.AdditionalQueryStringParameters ??= new Dictionary<string, string>();
            config.OAuth2Client.AdditionalQueryStringParameters["audience"] = auth0Options.Audience;
          }
        }

        config.ServerUrl = "https://metazoa-t.api.zoopraha.cz";
      });
    }

    app.UseAuthentication();
    app.UseAuthorization();

    RegisterEndpoints(app);

    app.MapControllers();

    app.Run();
  }
}