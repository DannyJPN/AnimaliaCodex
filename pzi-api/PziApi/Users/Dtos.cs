namespace PziApi.Users;

public class Dtos
{
  public record UserLoggedInRequest(
    string UserName,
    string? TenantId = null,
    string[]? Roles = null,
    string[]? Permissions = null
  );

  public record UserSettingsModel(
    int UserId,
    string UserName,
    string[] VisibleTaxonomyStatuses,
    bool TaxonomySearchByCz,
    bool TaxonomySearchByLat,
    string[] Permissions
  );

  public record UserSettingsUpdateModel(
    string UserName,
    string[] VisibleTaxonomyStatuses,
    bool TaxonomySearchByCz,
    bool TaxonomySearchByLat,
    int[] FlaggedSpecies,
    int[] FlaggedDistricts
  );
}
