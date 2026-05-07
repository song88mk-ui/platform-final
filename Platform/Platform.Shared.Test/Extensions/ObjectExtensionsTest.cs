using Platform.Shared.Extensions;
using Platform.Shared.MessageModels;
using Xunit;

namespace Platform.Shared.Test.Extensions
{
    public class ObjectExtensionsTest
    {
        private const string PrivateKey = "fjfOLes3FEbe2ubdqFtIak4CFDXRk6Lg";

        [Fact]
        public void ToSignOG_ShouldWork_WithComplexRecordsStructure()
        {
            // Arrange
            var model = CreateModel();

            // Act
            var hash = model.ToStringOG();
            var signature = hash.ToSignOG(PrivateKey);

            // Assert
            Assert.Equal(model.Signature, signature);
        }

        [Fact]
        public void ToSignOG_ShouldWork_WithSuccessPayload()
        {
            // Arrange
            var model = CreateModelSuccess();

            // Act
            var hash = model.ToStringOG();
            var signature = hash.ToSignOG(PrivateKey);

            // Assert
            Assert.Equal(model.Signature, signature);
        }
        [Fact]
        public void ToSignOG_ShouldWork_WithSuccessPayload2()
        {
            // Arrange
            var model = CreateModel2();

            // Act
            var hash = model.ToStringOG();
            var signature = hash.ToSignOG(PrivateKey);

            // Assert
            Assert.Equal(model.Signature, signature);
        }
        private OnOGDebitRequest CreateModel2()
        {
            var records = new[]
            {
                new
                {
                    amount = 8.15m,
                    bet_place = "single",
                    transaction_id = "09084da67c7840db36c608de9c62d537",
                    secondary_info = new
                    {
                        bet_amount = 8.15m,
                        detain_amount = 8.15m
                    },
                    other_info = new
                    {
                        BetId = "09084da6-7c78-40db-36c6-08de9c62d537",
                        UserId = "609_SilverKoala690",
                        CurrencyCode = "CNY",
                        bet_status = "Running",
                        ParlayBonus = 0,
                        ParlayBonusWinnings = 0,
                        odds_type = "Decimal",
                        total_odds = 1.22,
                        parlay_type = (string)null,
                        bet_list = new[]
                        {
                            new
                            {
                                sport_code = "17",
                                sport_name = new { en = "American Football" },
                                play_name = new { en = "Money Line (Including Overtime)" },
                                score = "0 - 0",
                                odds = 1.22,
                                original_odds = 1.22,
                                bet_item = new { en = "[Test] New York Giants D" },
                                away_team = new { en = "[Test] New England Patriots D" },
                                home_team = new { en = "[Test] New York Giants D" },
                                league_name = new { en = "NFL GSIS Replay Manager Season 2023" },
                                game_date_time = 1777550400000,
                                BetSelectionId = "c6668b6d-d188-47fa-c8c1-08de9c62d55e",
                                bet_selection_status = "Running",
                                SegmentId = 3795038,
                                SegmentName = "Test Region",
                                LeagueId = 90000985,
                                FixtureId = "1497027313257168896",
                                FixtureName = "[Test] New York Giants D vs. [Test] New England Patriots D",
                                FixtureStartDate = "2026-04-30T12:00:00.0000000",
                                FixtureStatus = "Live",
                                FixtureType = "Match",
                                settlement_score = "0 - 0",
                                MarketTemplateId = "103",
                                MarketId = "1499380103231401985",
                                Line = "",
                                SelectionId = "1499380103235596291",
                                DeadHeatFactor = 1
                            }
                        }
                    },
                    remark = (string)null
                }
            };

            return new OnOGDebitRequest
            {
                PlayerCode = "609_SilverKoala690",
                TimeStamp = 1777554346540,
                TotalAmount = 8.15m,
                TransactionType = "debit",
                GameCode = "ogsports",
                RoundId = "na",
                Currency = "CNY",
                CalledAt = 1777554346540,
                Records = records,
                Signature = "b152697050c12cde43477b76ed11874b"
            };
        }
        private OnOGDebitRequest CreateModelSuccess()
        {
            var records = new[]
            {
                new
                {
                    amount = 8.15m,
                    bet_place = "single",
                    transaction_id = "474dcf1255814c42cbbb08de943d2558",
                    secondary_info = new
                    {
                        bet_amount = 8.15m,
                        detain_amount = 8.15m
                    },
                    other_info = new
                    {
                        BetId = "474dcf12-5581-4c42-cbbb-08de943d2558",
                        UserId = "609_KhakiKatydid015",
                        CurrencyCode = "CNY",
                        bet_status = "Running",
                        ParlayBonus = 0,
                        ParlayBonusWinnings = 0,
                        odds_type = "Decimal",
                        total_odds = 1.85,
                        parlay_type = (string)null,
                        bet_list = new[]
                        {
                            new
                            {
                                sport_code = "17",
                                sport_name = new { en = "American Football" },
                                play_name = new { en = "Total Points Over/Under (Including Overtime) 54.50" },
                                score = "27 - 7",
                                odds = 1.85,
                                original_odds = 1.85,
                                bet_item = new { en = "Over" },
                                away_team = new { en = "[Test] Washington Commanders D" },
                                home_team = new { en = "[Test] New York Jets D" },
                                league_name = new { en = "NFL GSIS Replay Manager Season 2023" },
                                game_date_time = 1775656800000,
                                BetSelectionId = "dea45c76-f933-4794-fc2d-08de943d257d",
                                bet_selection_status = "Running",
                                SegmentId = 3795038,
                                SegmentName = "Test Region",
                                LeagueId = 90000985,
                                FixtureId = "1490615987286286336",
                                FixtureName = "[Test] New York Jets D vs. [Test] Washington Commanders D",
                                FixtureStartDate = "2026-04-08T14:00:00.0000000",
                                FixtureStatus = "Live",
                                FixtureType = "Match",
                                settlement_score = "27 - 7",
                                MarketTemplateId = "263",
                                MarketId = "1491461217824776193",
                                Line = "54.50",
                                SelectionId = "1491461217824776195",
                                DeadHeatFactor = 1
                            }
                        }
                    },
                    remark = (string)null
                }
            };

            return new OnOGDebitRequest
            {
                PlayerCode = "609_KhakiKatydid015",
                TimeStamp = 1775662774782,
                TotalAmount = 8.15m,
                TransactionType = "debit",
                GameCode = "ogsports",
                RoundId = "na",
                Currency = "CNY",
                CalledAt = 1775662774782,
                Records = records,
                Signature = "369ef857753fe17083b81338b30229c9"
            };
        }
        private OnOGDebitRequest CreateModel()
        {
            var records = new[]
            {
                new
                {
                    amount = 8.15m,
                    bet_place = "single",
                    transaction_id = "a9412a74304f46fef73a08de9ae6f0da",
                    secondary_info = new
                    {
                        bet_amount = 8.15m,
                        detain_amount = 8.15m
                    },
                    other_info = new
                    {
                        BetId = "a9412a74-304f-46fe-f73a-08de9ae6f0da",
                        UserId = "609_PeekingQuokka442",
                        CurrencyCode = "CNY",
                        bet_status = "Rollbacked",
                        ParlayBonus = 0,
                        ParlayBonusWinnings = 0,
                        odds_type = "Decimal",
                        total_odds = 1.19,
                        parlay_type = (string)null,
                        bet_list = new[]
                        {
                            new
                            {
                                sport_code = "6",
                                sport_name = new { en = "Cricket" },
                                play_name = new { en = "Match Winner" },
                                score = "231/1 (41.1/50) - 0/0 (0.0/50)",
                                odds = 1.19,
                                original_odds = 1.19,
                                bet_item = new { en = "Durham Women" },
                                away_team = new { en = "Essex Women" },
                                home_team = new { en = "Durham Women" },
                                league_name = new { en = "England One Day Cup Women" },
                                game_date_time = 1776245400000,
                                BetSelectionId = "50260a74-20ff-435e-5926-08de9ae6f104",
                                bet_selection_status = "Rollbacked",
                                SegmentId = 217,
                                SegmentName = "Great Britain and Northern Ireland",
                                LeagueId = 21505,
                                FixtureId = "1493374156521578496",
                                FixtureName = "Durham Women vs. Essex Women",
                                FixtureStartDate = "2026-04-15T09:30:00.0000000",
                                FixtureStatus = "Live",
                                FixtureType = "Match",
                                settlement_score = "231/1 (41.1/50) - 0/0 (0.0/50)",
                                MarketTemplateId = "8884",
                                MarketId = "1493522048112631809",
                                Line = "",
                                SelectionId = "1493522048121020422",
                                DeadHeatFactor = 1
                            }
                        }
                    },
                    remark = (string)null
                }
            };

            return new OnOGDebitRequest
            {
                PlayerCode = "609_PeekingQuokka442",
                TimeStamp = 1776254803468,
                TotalAmount = 8.15m,
                TransactionType = "rollback",
                GameCode = "ogsports",
                RoundId = "na",
                Currency = "CNY",
                CalledAt = 1776254803468,
                Records = records,
                Signature = "d2b7f301870e3b46d8ba4421abf98f58"
            };
        }
    }
}
