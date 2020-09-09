using BoilerPlate.ServiceLayer.Security.Models;
using System.Threading.Tasks;

namespace BoilerPlate.ServiceLayer.Security.Interface
{
    public interface  IRefreshTokenService
    {
        Task<JwtWithRefreshToken> Refresh(string token, string refreshToken);
        string GenerateAndSaveRefreshToken(string identityUserId);
    }
}
