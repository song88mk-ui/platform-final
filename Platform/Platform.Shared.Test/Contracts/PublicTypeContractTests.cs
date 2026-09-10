using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Platform.Shared.Test.Contracts
{
    // Tier 3 of the test plan: blanket coverage for the data-holder half of Platform.Shared.
    //
    // Roughly 430 of the assembly's public classes are DTOs, EF entities and request/response
    // models - types with no behaviour beyond properties. Hand-writing a test class per type would
    // produce hundreds of files of assertions that can never fail. Instead these tests sweep every
    // such type reflectively and assert the properties that actually matter for a data holder:
    // it can be constructed, no getter throws on a fresh instance, and a value written to a simple
    // property comes back unchanged.
    //
    // What this does NOT prove: any behaviour. It catches a property that lost its setter, an
    // initialiser that throws, and a type that stopped being constructible. Do not read it as
    // meaningful coverage of the assembly - the behaviour lives in the tiers above.
    public class PublicTypeContractTests
    {
        private static readonly Type[] SimpleValueTypes =
        {
            typeof(string), typeof(bool), typeof(byte), typeof(short), typeof(int), typeof(long),
            typeof(float), typeof(double), typeof(decimal), typeof(DateTime), typeof(DateTimeOffset),
            typeof(Guid), typeof(TimeSpan), typeof(char)
        };

        private static IEnumerable<Type> DataHolderTypes =>
            typeof(Platform.Shared.MessageModels.OnBetStatus).Assembly
                .GetTypes()
                .Where(t => t.IsPublic && t.IsClass && !t.IsAbstract && !t.IsNested)
                .Where(t => t.Namespace != null)
                .Where(t => t.Namespace!.StartsWith("Platform.Shared.Models", StringComparison.Ordinal)
                            || t.Namespace!.StartsWith("Platform.Shared.Data", StringComparison.Ordinal)
                            || t.Namespace!.StartsWith("Platform.Shared.MessageModels", StringComparison.Ordinal))
                .Where(t => !t.ContainsGenericParameters)
                .Where(t => !t.Name.EndsWith("Context", StringComparison.Ordinal))
                .Where(t => t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => t.FullName, StringComparer.Ordinal);

        [Fact]
        public void DataHolderTypes_AreDiscovered()
        {
            // Guards the filter: without this, a namespace rename would make the sweep below pass
            // over an empty set.
            Assert.True(DataHolderTypes.Count() >= 200, $"expected at least 200 data-holder types, found {DataHolderTypes.Count()}");
        }

        [Fact]
        public void EveryDataHolderType_ConstructsWithoutThrowing()
        {
            var offenders = new List<string>();

            foreach (var type in DataHolderTypes)
            {
                try
                {
                    Activator.CreateInstance(type);
                }
                catch (Exception ex)
                {
                    offenders.Add($"{type.FullName}: {ex.GetBaseException().GetType().Name}");
                }
            }

            Assert.Empty(offenders);
        }

        [Fact]
        public void NoPropertyGetter_ThrowsOnAFreshInstance()
        {
            // A computed property that dereferences an uninitialised collection throws the moment
            // anything serialises the object - which, on a message model, means at publish time.
            var offenders = new List<string>();

            foreach (var type in DataHolderTypes)
            {
                object? instance;

                try
                {
                    instance = Activator.CreateInstance(type);
                }
                catch
                {
                    continue; // reported by the construction test above
                }

                foreach (var property in ReadableProperties(type))
                {
                    try
                    {
                        property.GetValue(instance);
                    }
                    catch (Exception ex)
                    {
                        offenders.Add($"{type.FullName}.{property.Name}: {ex.GetBaseException().GetType().Name}");
                    }
                }
            }

            Assert.Empty(offenders);
        }

        [Fact]
        public void EverySimpleProperty_RoundTripsTheValueItWasGiven()
        {
            // Catches a setter that silently drops or transforms its input - for example a property
            // whose setter was refactored into a no-op, or one backed by the wrong field.
            var offenders = new List<string>();

            foreach (var type in DataHolderTypes)
            {
                object? instance;

                try
                {
                    instance = Activator.CreateInstance(type);
                }
                catch
                {
                    continue;
                }

                foreach (var property in ReadableProperties(type).Where(p => p.CanWrite && IsSimple(p.PropertyType)))
                {
                    var value = SampleValue(property.PropertyType);

                    try
                    {
                        property.SetValue(instance, value);

                        if (!Equals(property.GetValue(instance), value))
                            offenders.Add($"{type.FullName}.{property.Name}");
                    }
                    catch (Exception ex)
                    {
                        offenders.Add($"{type.FullName}.{property.Name}: {ex.GetBaseException().GetType().Name}");
                    }
                }
            }

            Assert.Empty(offenders);
        }

        private static IEnumerable<PropertyInfo> ReadableProperties(Type type) =>
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

        private static bool IsSimple(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;

            return underlying.IsEnum || SimpleValueTypes.Contains(underlying);
        }

        private static object SampleValue(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;

            if (underlying.IsEnum)
                return Enum.GetValues(underlying).Cast<object>().First();

            if (underlying == typeof(string)) return "sample";
            if (underlying == typeof(bool)) return true;
            if (underlying == typeof(byte)) return (byte)7;
            if (underlying == typeof(short)) return (short)7;
            if (underlying == typeof(int)) return 7;
            if (underlying == typeof(long)) return 7L;
            if (underlying == typeof(float)) return 7.5f;
            if (underlying == typeof(double)) return 7.5d;
            if (underlying == typeof(decimal)) return 7.5m;
            if (underlying == typeof(DateTime)) return new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            if (underlying == typeof(DateTimeOffset)) return new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            if (underlying == typeof(Guid)) return Guid.Parse("11111111-2222-3333-4444-555555555555");
            if (underlying == typeof(TimeSpan)) return TimeSpan.FromMinutes(7);
            if (underlying == typeof(char)) return 'x';

            throw new InvalidOperationException($"no sample value for {underlying.Name}");
        }
    }
}
