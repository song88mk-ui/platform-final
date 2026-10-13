using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace Platform.Betting.API.IntegrationTests.TestSupport
{
    /// <summary>
    /// Builds the song88 SQL schema from the dev Flyway migrations in the infrastructure repo.
    /// </summary>
    /// <remarks>
    /// There is no other schema source: platform-final has no EF migrations, and its DbContexts are
    /// scaffolded subsets that carry no stored procedures. The migrations use three-part names
    /// (<c>Betting.dbo.Bet</c>), so every database they touch is created first.
    ///
    /// Replays them the way Flyway does: versioned <c>V*.sql</c> in version order, then repeatable
    /// <c>R__*.sql</c> in name order, each split into batches on <c>GO</c> lines and run on one
    /// connection so a <c>USE</c> carries over to the next batch. SqlClient opens connections with
    /// QUOTED_IDENTIFIER ON; V1, V5 and V8 fail with Msg 1934 without it (DEVOPS-408 spike).
    ///
    /// The directory comes from <c>SONG88_FLYWAY_MIGRATIONS</c>, else from the sibling
    /// <c>infrastructure</c> clone next to platform-final.
    /// </remarks>
    public static class FlywayMigrations
    {
        public const string EnvironmentVariable = "SONG88_FLYWAY_MIGRATIONS";

        public static readonly string[] Databases =
            ["Auth", "Backoffice", "Betting", "Platform", "Sportsbook", "Trading", "Translation", "Wallet"];

        private static readonly Regex GoLine = new(@"^\s*GO\s*;?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private static readonly Regex VersionPrefix = new(@"^V(\d+)__", RegexOptions.IgnoreCase);

        public static async Task ApplyAsync(string masterConnectionString)
        {
            var directory = LocateDirectory();

            await using var connection = new SqlConnection(masterConnectionString);
            await connection.OpenAsync();

            foreach (var database in Databases)
                await ExecuteAsync(connection, $"IF DB_ID(N'{database}') IS NULL CREATE DATABASE [{database}];", "create databases");

            foreach (var file in OrderedScripts(directory))
            {
                var script = await File.ReadAllTextAsync(file);
                foreach (var batch in GoLine.Split(script).Where(b => !string.IsNullOrWhiteSpace(b)))
                    await ExecuteAsync(connection, batch, Path.GetFileName(file));
            }
        }

        public static IEnumerable<string> OrderedScripts(string directory)
        {
            var files = Directory.GetFiles(directory, "*.sql");

            var versioned = files
                .Select(f => (File: f, Match: VersionPrefix.Match(Path.GetFileName(f))))
                .Where(x => x.Match.Success)
                .OrderBy(x => int.Parse(x.Match.Groups[1].Value))
                .Select(x => x.File);

            var repeatable = files
                .Where(f => Path.GetFileName(f).StartsWith("R__", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal);

            return versioned.Concat(repeatable);
        }

        public static string LocateDirectory()
        {
            var configured = Environment.GetEnvironmentVariable(EnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(configured))
            {
                if (Directory.Exists(configured))
                    return configured;
                throw new DirectoryNotFoundException($"{EnvironmentVariable} is set to '{configured}', which does not exist.");
            }

            // Walk up from bin/<config>/<tfm> until a directory holds the infrastructure clone.
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "infrastructure", "flyway", "migrations", "dev");
                if (Directory.Exists(candidate))
                    return candidate;
            }

            throw new DirectoryNotFoundException(
                $"Dev Flyway migrations not found. Clone the infrastructure repo next to platform-final, " +
                $"or set {EnvironmentVariable} to its flyway/migrations/dev directory.");
        }

        private static async Task ExecuteAsync(SqlConnection connection, string sql, string source)
        {
            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 300 };
            try
            {
                await command.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Migration '{source}' failed: {ex.Message}", ex);
            }
        }
    }
}
