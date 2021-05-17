using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
{
    public interface ICashoutRepository
    {
        Task<BaseResponseDTO<bool>> CashoutBet(CashoutBetRequestDTO request);
    }
}
