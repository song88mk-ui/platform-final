using Platform.Operator.API.Models;
using System.Threading.Tasks;

namespace Platform.Operator.API.Repository
{
    public interface IReportsRepository
    {
        Task<RunningBalanceResponse> GetRunningBalance(string userId);
    }
}
