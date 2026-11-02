using MongoDB.Driver;
using Platform.Shared.Data.Sportsbook.Entities;
using Platform.Shared.Enums;

namespace Platform.Sportsbook.API.Utility
{
    // The Fixture fields SportsbookRepository.GetSports (the sports tree) reads, projected into
    // SportsTreeFixtureProjection. The tree names sports, segments and leagues only, so the team name
    // lists (HoNaTr, AwNaTr) are not read; fields left out deserialize to null, which the translation
    // helpers already treat as "no translation".
    //
    // ClCo is read only by the client-code filter behind IsOEEnable; it stays so that filter keeps
    // working if it is switched back on.
    //
    // Non-EN requests resolve names through SpNaTr, SegNaTr and LeNaTr, so those come back cut to the
    // requested language with $elemMatch, as TopLeaguesFixtureProjection does. EN never reads them.
    public static class SportsTreeProjection
    {
        private static readonly ProjectionDefinition<Fixture> Fields = Builders<Fixture>.Projection
            .Include(c => c.Id)
            .Include(c => c.FiId)
            .Include(c => c.ClCo)
            .Include(c => c.SpId)
            .Include(c => c.SpNa)
            .Include(c => c.SegId)
            .Include(c => c.SegNa)
            .Include(c => c.LeId)
            .Include(c => c.LeNa)
            .Include(c => c.FiStDa)
            .Include(c => c.FiStId)
            .Include(c => c.FiTyId);

        public static ProjectionDefinition<Fixture> For(LanguageCode languageCode)
        {
            if (languageCode == LanguageCode.EN)
                return Fields;

            var language = languageCode.ToString();

            return Builders<Fixture>.Projection.Combine(
                Fields,
                Builders<Fixture>.Projection.ElemMatch(c => c.SpNaTr, t => t.LaCo == language),
                Builders<Fixture>.Projection.ElemMatch(c => c.SegNaTr, t => t.LaCo == language),
                Builders<Fixture>.Projection.ElemMatch(c => c.LeNaTr, t => t.LaCo == language));
        }
    }
}
