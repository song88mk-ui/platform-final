using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Job.Operator.Score.Repository
{
    public interface ICricketScoreRepository
    {
        Task<string> GetScoreSummary(string fixtureSpecifier);
    }
}
