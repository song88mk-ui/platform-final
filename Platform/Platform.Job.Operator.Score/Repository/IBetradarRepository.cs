using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Job.Operator.Score.Repository
{
    public interface IBetradarRepository
    {
        Task<string> GetScoreSummary(string fixtureSpecifier);
    }
}
