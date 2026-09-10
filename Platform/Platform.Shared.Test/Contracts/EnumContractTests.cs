using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Platform.Shared.Enums;
using Xunit;

namespace Platform.Shared.Test.Contracts
{
    // Companion to MessageContractTests for the other half of the R-2 contract surface: the enums
    // in Platform.Shared/Enums. Their NUMERIC values travel over RabbitMQ, sit in SQL columns, and
    // are read by odds-engine-2, platform-web and backoffice-final. Inserting a member in the
    // middle of an enum silently renumbers everything after it, which reinterprets historical
    // rows and in-flight messages without any compile error anywhere.
    //
    // The snapshot in Contracts/enum-contracts.approved.txt pins every member's name and numeric
    // value. Appending a new member at the end is a safe change; a failure on an existing member
    // means values moved and needs coordinating with the sibling repos and the database.
    public class EnumContractTests
    {
        private const string ApprovedFileName = "enum-contracts.approved.txt";

        private static IEnumerable<Type> EnumTypes =>
            typeof(BetStatus).Assembly
                .GetTypes()
                .Where(t => t.Namespace == "Platform.Shared.Enums")
                .Where(t => t.IsPublic && t.IsEnum && !t.IsNested)
                .OrderBy(t => t.Name, StringComparer.Ordinal);

        [Fact]
        public void EnumContracts_MatchTheApprovedSnapshot()
        {
            var approvedPath = Path.Combine(AppContext.BaseDirectory, "Contracts", ApprovedFileName);

            Assert.True(File.Exists(approvedPath),
                $"Approved enum snapshot not found at {approvedPath}. It must be checked in; see the class comment.");

            var expected = Normalise(File.ReadAllText(approvedPath));
            var actual = Normalise(BuildSnapshot());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void NoEnum_HasDuplicateNumericValues()
        {
            // Two names sharing a value make the reverse mapping ambiguous, so a round trip through
            // the number cannot recover which member was meant.
            var offenders = new List<string>();

            foreach (var type in EnumTypes)
            {
                var values = Enum.GetValues(type).Cast<object>().Select(v => Convert.ToInt64(v)).ToList();

                if (values.Count != values.Distinct().Count())
                    offenders.Add(type.Name);
            }

            Assert.Empty(offenders);
        }

        [Fact]
        public void EnumTypes_AreDiscovered()
        {
            Assert.True(EnumTypes.Count() >= 30, $"expected at least 30 enums, found {EnumTypes.Count()}");
        }

        internal static string BuildSnapshot()
        {
            var builder = new StringBuilder();

            foreach (var type in EnumTypes)
            {
                builder.Append(type.Name).Append(" : ").Append(Enum.GetUnderlyingType(type).Name).Append('\n');

                var members = Enum.GetNames(type)
                    .Select(name => new { Name = name, Value = Convert.ToInt64(Enum.Parse(type, name)) })
                    .OrderBy(m => m.Value)
                    .ThenBy(m => m.Name, StringComparer.Ordinal);

                foreach (var member in members)
                {
                    builder.Append("  ").Append(member.Name).Append(" = ").Append(member.Value).Append('\n');
                }
            }

            return builder.ToString();
        }

        private static string Normalise(string value) =>
            value.Replace("\r\n", "\n").Replace("\r", "\n").TrimEnd('\n');
    }
}
