namespace Platform.Job.BetAudit.Tableau.Repository
{
    public interface ITableauRepository
    {
        void ProcessBets();
        void ProcessBetSelections();
    }
}
