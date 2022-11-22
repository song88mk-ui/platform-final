using System.Threading.Tasks;
using System;
using System.Net;

namespace Platform.Sportsbook.API.Repository
{
    public interface IBetBuilderRepository
    {
        Task<Tuple<HttpStatusCode, string>> ValidateBetBuilderCombination(string payload);
    }
}
