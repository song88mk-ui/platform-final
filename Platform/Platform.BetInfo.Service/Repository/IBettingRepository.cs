using Platform.Shared.MessageModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.BetInfo.Service.Repository
{
    public interface IBettingRepository
    {
        void ProcessBetPlacement(OnBetPlacement message);
    }
}
