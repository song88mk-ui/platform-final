using Platform.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Platform.Sportsbook.API.Repository
{
    public interface ITranslationRepository
    {
        Task<Dictionary<string, string>> GetTranslationsAsync(
            List<string> stringIds,
            string languageCode);

        void InvalidateTranslationsCache(
            List<string> stringIds,
            string languageCode);
    }
}
