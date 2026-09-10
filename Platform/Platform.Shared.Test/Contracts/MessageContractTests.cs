using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Platform.Shared.MessageModels;
using Xunit;

namespace Platform.Shared.Test.Contracts
{
    // Closes the gap recorded as R-2 in docs/ArchitectureRisks.md: the RabbitMQ message contracts
    // in Platform.Shared/MessageModels are duplicated by convention across platform-final,
    // odds-engine-2, platform-web, oddsfeed and backoffice-final, with no shared package and no
    // contract test. Renaming or removing a property is a silent break in up to four other repos,
    // and nothing in this solution fails when it happens.
    //
    // This test is that missing alarm. It snapshots every message type's serialised property names
    // into Contracts/message-contracts.approved.txt and fails when the live shape drifts from it.
    // A failure is not automatically a bug - it means the wire format changed, and the change must
    // be coordinated with the sibling repos before the snapshot is updated.
    //
    // To accept a deliberate change: run the test, copy the "actual" block from the failure
    // message into the approved file, and note the coordinating ticket in the commit message.
    public class MessageContractTests
    {
        private const string ApprovedFileName = "message-contracts.approved.txt";

        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions();

        private static IEnumerable<Type> MessageTypes =>
            typeof(OnBetStatus).Assembly
                .GetTypes()
                .Where(t => t.Namespace == "Platform.Shared.MessageModels")
                .Where(t => t.IsPublic && t.IsClass && !t.IsAbstract && !t.IsNested)
                .OrderBy(t => t.Name, StringComparer.Ordinal);

        [Fact]
        public void MessageContracts_MatchTheApprovedSnapshot()
        {
            var approvedPath = Path.Combine(AppContext.BaseDirectory, "Contracts", ApprovedFileName);

            Assert.True(File.Exists(approvedPath),
                $"Approved contract snapshot not found at {approvedPath}. It must be checked in; see the class comment.");

            var expected = Normalise(File.ReadAllText(approvedPath));
            var actual = Normalise(BuildSnapshot());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void EveryMessageType_HasAPublicParameterlessConstructor()
        {
            // The broker deserialises into these types, which requires one.
            var offenders = MessageTypes
                .Where(t => t.GetConstructor(Type.EmptyTypes) == null)
                .Select(t => t.Name)
                .ToList();

            Assert.Empty(offenders);
        }

        [Fact]
        public void EveryMessageType_RoundTripsThroughSystemTextJson()
        {
            foreach (var type in MessageTypes)
            {
                var instance = Activator.CreateInstance(type);

                var json = JsonSerializer.Serialize(instance, type, SerializerOptions);
                var restored = JsonSerializer.Deserialize(json, type, SerializerOptions);

                Assert.NotNull(restored);
                Assert.Equal(json, JsonSerializer.Serialize(restored, type, SerializerOptions));
            }
        }

        [Fact]
        public void EveryMessageType_SerialisesEveryPublicReadWriteProperty()
        {
            // Catches an accidental [JsonIgnore], a private setter, or a property renamed only on
            // the serialisation side - each of which drops a field off the wire silently.
            foreach (var type in MessageTypes)
            {
                var instance = Activator.CreateInstance(type);
                var json = JsonSerializer.Serialize(instance, type, SerializerOptions);

                using var document = JsonDocument.Parse(json);
                var serialisedNames = document.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

                // Several OG models rename properties on the wire with [JsonPropertyName], so the
                // effective JSON name is what must be present, not the CLR name.
                var expectedNames = ReadWriteProperties(type)
                    .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                    .Select(JsonName)
                    .ToList();

                var missing = expectedNames.Where(n => !serialisedNames.Contains(n)).ToList();

                Assert.Empty(missing);
            }
        }

        [Fact]
        public void MessageTypes_AreDiscovered()
        {
            // Guards the reflection filter itself: if the namespace is renamed or the filter stops
            // matching, every other test in this class would pass vacuously.
            Assert.True(MessageTypes.Count() >= 20, $"expected at least 20 message types, found {MessageTypes.Count()}");
        }

        // The wire name: [JsonPropertyName] when present, otherwise the CLR name, since no custom
        // naming policy is configured on the serializer used by the broker.
        private static string JsonName(PropertyInfo property) =>
            property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;

        private static IEnumerable<PropertyInfo> ReadWriteProperties(Type type) =>
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
                .OrderBy(p => p.Name, StringComparer.Ordinal);

        internal static string BuildSnapshot()
        {
            var builder = new StringBuilder();

            foreach (var type in MessageTypes)
            {
                builder.Append(type.Name).Append('\n');

                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.GetIndexParameters().Length == 0)
                             .OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    builder.Append("  ")
                        .Append(property.Name)
                        .Append(" -> ")
                        .Append(property.GetCustomAttribute<JsonIgnoreAttribute>() == null ? JsonName(property) : "(ignored)")
                        .Append(": ")
                        .Append(TypeName(property.PropertyType))
                        .Append('\n');
                }
            }

            return builder.ToString();
        }

        private static string TypeName(Type type)
        {
            if (!type.IsGenericType)
                return type.Name;

            var arguments = string.Join(",", type.GetGenericArguments().Select(TypeName));
            var name = type.Name;
            var tickIndex = name.IndexOf('`');

            if (tickIndex >= 0)
                name = name.Substring(0, tickIndex);

            return $"{name}<{arguments}>";
        }

        private static string Normalise(string value) =>
            value.Replace("\r\n", "\n").Replace("\r", "\n").TrimEnd('\n');
    }
}
