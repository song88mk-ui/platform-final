using Platform.API.Models;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    public interface IPlatformRepository
    {
        Task<AuthenticationResponse> Authenticate(string operatorToken, string endpoint);
        Task<ClientResponse> GetClient(string clientId);
        Task<WalletBalanceResponse> GetWalletBalance(decimal balance);
    }
}
