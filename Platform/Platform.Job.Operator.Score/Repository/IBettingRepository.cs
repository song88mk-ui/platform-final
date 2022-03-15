using Platform.Shared.Data.Betting;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using System;
using System.Threading.Tasks;

namespace Platform.Job.Operator.Score.Repository
{
    public interface IBettingRepository
    {
        void PreloadSettledBets();
        Task ProcessSettledBets();
    }
}
