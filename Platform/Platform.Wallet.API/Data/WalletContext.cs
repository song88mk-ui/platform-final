using Microsoft.EntityFrameworkCore;

namespace Platform.Wallet.API.Data
{
    public class WalletContext : DbContext
    {
        public WalletContext(DbContextOptions<WalletContext> options) : base(options)
        {
        }
    }
}
