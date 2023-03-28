using Platform.Wallet.API.Models;
using System;
using System.Threading.Tasks;

namespace Platform.Wallet.API.Repository
{
    public interface IReportsRepository
    {
        Task<RunningBalanceResponse> GetRunningBalance(string userId);
    }
}
