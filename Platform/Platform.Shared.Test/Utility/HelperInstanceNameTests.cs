using System;
using Platform.Shared.Utility;
using Xunit;

namespace Platform.Shared.Test.Utility
{
    // Covers Helper.GetInstanceName, which shards a provider fixture id onto an instance name by
    // reading only its last two characters:
    //
    //   last       = the final character minus '0'
    //   secondLast = the character before it, minus '0'
    //   baseDigit  = (last / 2) * 2        -> 0, 2, 4, 6, 8
    //   result     = secondLast is even ? baseDigit : baseDigit + 1
    //
    // formatted "D2". The shard is therefore stable for a given fixture id, which is the property
    // callers depend on: the same fixture must always land on the same instance.
    //
    // The arithmetic is character-based, not digit-parsing, so a non-digit character produces a
    // large number rather than an error. That is pinned below because it is surprising, not
    // because it is desirable.
    public class HelperInstanceNameTests
    {
        private static readonly Helper Subject = new Helper();

        [Theory]
        [InlineData("1234", "05")]   // last 4 -> base 4, secondLast 3 odd  -> 5
        [InlineData("1244", "04")]   // last 4 -> base 4, secondLast 4 even -> 4
        [InlineData("0000", "00")]
        [InlineData("0099", "09")]   // last 9 -> base 8, secondLast 9 odd  -> 9
        [InlineData("0089", "08")]   // last 9 -> base 8, secondLast 8 even -> 8
        [InlineData("12", "03")]     // last 2 -> base 2, secondLast 1 odd  -> 3
        public void GetInstanceName_MapsTheLastTwoDigits(string fixtureId, string expected)
        {
            Assert.Equal(expected, Subject.GetInstanceName(fixtureId));
        }

        [Fact]
        public void GetInstanceName_AlwaysReturnsTwoCharacters()
        {
            for (var tens = 0; tens <= 9; tens++)
            {
                for (var units = 0; units <= 9; units++)
                {
                    var result = Subject.GetInstanceName($"fixture-{tens}{units}");

                    Assert.Equal(2, result.Length);
                }
            }
        }

        [Fact]
        public void GetInstanceName_ResultIsAlwaysBetweenZeroAndNine()
        {
            // baseDigit maxes at 8 and the odd branch adds 1, so the shard space is 00 to 09.
            for (var tens = 0; tens <= 9; tens++)
            {
                for (var units = 0; units <= 9; units++)
                {
                    var result = int.Parse(Subject.GetInstanceName($"{tens}{units}"));

                    Assert.InRange(result, 0, 9);
                }
            }
        }

        [Fact]
        public void GetInstanceName_IgnoresEverythingBeforeTheLastTwoCharacters()
        {
            Assert.Equal(Subject.GetInstanceName("34"), Subject.GetInstanceName("999999934"));
            Assert.Equal(Subject.GetInstanceName("34"), Subject.GetInstanceName("sr:match:123456734"));
        }

        [Fact]
        public void GetInstanceName_IsStableForTheSameInput()
        {
            Assert.Equal(Subject.GetInstanceName("sr:match:48372615"), Subject.GetInstanceName("sr:match:48372615"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("7")]
        public void GetInstanceName_NullEmptyOrSingleCharacter_Throws(string fixtureId)
        {
            Assert.Throws<ArgumentException>(() => Subject.GetInstanceName(fixtureId));
        }

        [Fact]
        public void GetInstanceName_RejectionNamesTheReason()
        {
            var exception = Assert.Throws<ArgumentException>(() => Subject.GetInstanceName("7"));

            Assert.Contains("Invalid fixture id", exception.Message);
        }

        [Fact]
        public void GetInstanceName_NonDigitCharacters_ProduceAnOutOfRangeShard()
        {
            // 'a' and 'b' are 49 and 50 once '0' is subtracted, so the formula runs on them
            // happily and returns a two-digit-plus result well outside the 00-09 shard space.
            // Callers passing an id that does not end in digits get a shard no instance owns.
            var result = Subject.GetInstanceName("ab");

            Assert.Equal("51", result);
        }
    }
}
