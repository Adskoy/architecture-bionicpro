using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace Bionicpro;

/// <summary>
/// Перегоняет роли из клеммы realm_access в стандартный roles, с которым работает aspnet.
/// <remarks>
/// Вместо этого лучше настроить реалм keycloak отдавать роли реалма как roles
/// </remarks>
/// </summary>
public class ClaimsTransformer : IClaimsTransformation {
  public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal) {
    var identity = (principal.Identity as ClaimsIdentity)!;

    if (!identity.IsAuthenticated || !identity.HasClaim((claim) => claim.Type == "realm_access"))
      return Task.FromResult(principal);

    var claim = identity.FindFirst(x => x.Type == "realm_access");
    var claimDict = JsonSerializer.Deserialize<Dictionary<string, string[]>>(claim!.Value);
    foreach (var role in claimDict?["roles"] ?? []) {
      identity.AddClaim(new Claim(ClaimTypes.Role, role));
    }

    return Task.FromResult(principal);
  }
}