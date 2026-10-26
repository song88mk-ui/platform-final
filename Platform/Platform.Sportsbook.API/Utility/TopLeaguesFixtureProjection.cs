using MongoDB.Driver;
using Platform.Shared.Data.Sportsbook.Entities;
using Platform.Shared.Enums;

namespace Platform.Sportsbook.API.Utility
{
    // The Fixture fields SportsbookRepository.GetTopLeagues reads. A whole Fixture document also
    // carries every language's sport, fixture and team name lists, period scores and stream fields,
    // none of which the endpoint uses, so reading only these keeps the Mongo transfer and the BSON
    // deserialization proportional to what the response needs. Fields left out deserialize to their
    // defaults (null lists), which the translation helpers already treat as "no translation".
    //
    // ClCo is read only by the client-code filter behind IsOEEnable; it stays so that filter keeps
    // working if it is switched back on.
    //
    // Non-EN requests resolve segment and league names through SegNaTr and LeNaTr, so those come back
    // too, cut to the requested language with $elemMatch as the market and selection reads do. EN
    // never reads them. It lives here as a plain class so the projection can be unit-tested without a
    // Mongo instance, as SportsTreeFilter is.
    public static class TopLeaguesFixtureProjection
    {
        private static readonly ProjectionDefinition<Fixture> Fields = Builders<Fixture>.Projection
            .Include(c => c.SpId)
            .Include(c => c.SegId)
            .Include(c => c.SegNa)
            .Include(c => c.LeId)
            .Include(c => c.LeNa)
            .Include(c => c.FiStId)
            .Include(c => c.ClCo);

        public static ProjectionDefinition<Fixture> For(LanguageCode languageCode)
        {
            if (languageCode == LanguageCode.EN)
                return Fields;

            var language = languageCode.ToString();

            return Builders<Fixture>.Projection.Combine(
                Fields,
                Builders<Fixture>.Projection.ElemMatch(c => c.SegNaTr, t => t.LaCo == language),
                Builders<Fixture>.Projection.ElemMatch(c => c.LeNaTr, t => t.LaCo == language));
        }
    }
}
