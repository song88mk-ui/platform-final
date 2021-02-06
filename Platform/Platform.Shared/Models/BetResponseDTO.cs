using Platform.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.Models
{
    public class BetResponseDTO
    {
        public string UserId { get; set; }
        public string BetId { get; set; }
        public BetStatus BetStatus { get; set; }
    }
}
