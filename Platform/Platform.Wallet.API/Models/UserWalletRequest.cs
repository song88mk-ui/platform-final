using System;

namespace Platform.Wallet.API.Models
{
    public class UserWalletRequest
    {
        public Guid ClientId { get; set; }
        public string UserId { get; set; }
    }
}
