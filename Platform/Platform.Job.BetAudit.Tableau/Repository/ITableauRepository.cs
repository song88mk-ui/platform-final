namespace Platform.Job.BetAudit.Tableau.Repository
{
    public interface ITableauRepository
    {
        void ProcessBetsHK();
        void ProcessBetSelectionsHK();

        void ProcessBetsSA();
        void ProcessBetSelectionsSA();
    }
}
