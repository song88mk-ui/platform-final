using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Models
{
    public class BackOfficeBetConfigPayloadDTO
    {
        public Guid ClientId { get; set; }
        public List<BetConfigPayload> BetConfigPayload { get; set; }
    }

    public class BetConfigPayload
    {
        public string CurrencyCode { get; set; }
        public FootballBetSetting Football { get; set; }
        public AnySportBetSetting OtherSports { get; set; }
        public ParlayBetSetting Parlay { get; set; }
        public CricketBetSetting Cricket { get; set; }
        public PriceboostBetSetting Priceboost { get; set; }
        public TaxConfig TaxConfig { get; set; }
    }

    public class FootballBetSetting
    {
        public decimal MinBet { get; set; }
        public decimal MaxBet { get; set; }
        public decimal MaxFixtureBet { get; set; }
    }

    public class AnySportBetSetting
    {
        public decimal MinBet { get; set; }

        public decimal MaxBet { get; set; }

        public decimal MaxFixtureBet { get; set; }
    }

    public class ParlayBetSetting
    {
        public decimal MinBet { get; set; }

        public decimal MaxBet { get; set; }

        public decimal MaxPayout { get; set; }
        public decimal ParlayBonusMaxWinnings { get; set; }
        public decimal ParlayBonusMinOdds { get; set; }
        public List<ParlayBonusConfig> ParlayBonusConfig { get; set; }
    }

    public class CricketBetSetting
    {
        public decimal MinBet { get; set; }
        public decimal MaxBet { get; set; }
        public decimal MaxFixtureBet { get; set; }
    }

    public class PriceboostBetSetting
    {
        public decimal MaxStake { get; set; }
    }

    public class ParlayBonusConfig
    {
        public int SelectionCount { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal ParlayBonus { get; set; }
    }
}
