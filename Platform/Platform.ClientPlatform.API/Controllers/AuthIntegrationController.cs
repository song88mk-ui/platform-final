using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.ClientPlatform.API.ActionFilters;
using Platform.ClientPlatform.API.Repository;
using System.Threading.Tasks;

namespace Platform.ClientPlatform.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(AuthAPIKeyFilter))]
    public class AuthIntegrationController : IntegrationControllerBase
    {
        private readonly IPlatformRepository _platformRepository;

        public AuthIntegrationController(IPlatformRepository platformRepository)
        {
            _platformRepository = platformRepository;
        }
    }
}
