using System.Threading.Tasks;

namespace Platform.Job.CCF.Repository
{
    public interface IPlatformRepository
    {
        Task ProcessCCF();
    }
}
