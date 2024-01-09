using IdentityServer.Api.Common;
using IdentityServer.Api.Models;
using System.Threading.Tasks;

namespace IdentityServer.Api.Managers.AccountManager
{
    public interface IAccountManager
    {
        Task<StandardResponse<LoginViewModel>> GetLoginProviders(string returnUrl);
    }
}
