using BoilerPlate.ModelLayer.Identity;
using BoilerPlate.ServiceLayer.Security.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BoilerPlate.ServiceLayer.Security.Interface
{
    public interface  IJWTService
    {
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);

        Jwt GenerateJwtToken(ApplicationUser user);
        Task<JwtWithRefreshToken> GenerateJWtWithRefreshTokenAsync(ApplicationUser user );
    }
}
