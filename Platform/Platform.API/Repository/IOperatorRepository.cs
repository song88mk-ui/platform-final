using Platform.Shared.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    public interface IOperatorRepository
    {
        Task<HttpResponseMessage> Authenticate(string operatorToken, string endpoint);
    }
}
