using System;

namespace Platform.Operator.API.Models
{
    public class UserWalletRequest
    {
        public Guid ClientId { get; set; }
        public string UserId { get; set; }
    }
}
