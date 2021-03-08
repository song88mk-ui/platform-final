using System;
using System.Collections.Generic;

namespace Platform.Shared.MessageModels
{
    public class OnBOBetCancellaton
    {
        public OnBOBetCancellaton()
        {
            BetIds = new List<Guid>();
        }
        public List<Guid> BetIds { get; set; }
    }
}
