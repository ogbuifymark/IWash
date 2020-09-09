using BoilerPlate.ModelLayer.PresentationViewModels;
using BoilerPlate.ServiceLayer.Security.Models;
using System;
using System.Threading.Tasks;

namespace BoilerPlate.ServiceLayer.Security.Interface
{
    public interface  ILoginService
    {
         Task<Jwt> Login(LoginModel model, Func<string, string, string> generateConfirmationLink, string PathToEmailFile);
        Task<Jwt> LoginWithoutEmailConfirmation(LoginModel model);
    }
}
