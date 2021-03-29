using System.Threading.Tasks;

namespace Platform.Job.ForexEUR.Repository
{
    public interface IPlatformRepository
    {
        Task ProcessForex();
    }
}
