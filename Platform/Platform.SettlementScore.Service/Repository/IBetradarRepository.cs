using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.SettlementScore.Service.Repository
{
    public interface IBetradarRepository
    {
        Task<string> GetScoreSummary(string fixtureSpecifier);
    }
}
