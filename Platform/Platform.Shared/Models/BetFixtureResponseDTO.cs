using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BetFixtureResponseDTO
    {
        public BetFixtureResponseDTO()
        {
            Selections = new List<BetFixtureSelectionResponseDTO>();
        }
        public List<BetFixtureSelectionResponseDTO> Selections { get; set; }
    }

    public class BetSelectionProbabilityResponseDTO
    {
        public BetSelectionProbabilityResponseDTO()
        {
            Selections = new List<BetSelectionProbabilityDTO>();
        }
        public List<BetSelectionProbabilityDTO> Selections { get; set; }
    }

    public class BetSelectionProbabilityDTO
    {
        public long SelectionId { get; set; }
        public int FixtureStatusId { get; set; }
        public int MarketStatusId { get; set; }
        public bool IsGlobalBetStop { get; set; }
        public bool IsSuspended { get; set; }
        public bool IsActive { get; set; }
        public double Probability { get; set; }
        public double WonProbability { get; set; }
        public double LostProbability { get; set; }
        public double VoidProbability { get; set; }
        public double HalfWonProbability { get; set; }
        public double HalfLostProbability { get; set; }
        public int SelectionStatusId { get; set; }
    }

    public class BetFixtureSelectionResponseDTO
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public long FixtureId { get; set; }
        public string FixtureName { get; set; }
        public FixtureType FixtureType { get; set; }
        public string MarketTemplateId { get; set; }
        public long MarketId { get; set; }
        public string MarketName { get; set; }
        public string Line { get; set; }
        public string SelectionTemplateId { get; set; }
        public long SelectionId { get; set; }
        public string SelectionName { get; set; }
        public decimal Odds { get; set; }
        public FixtureStatus FixtureStatus { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public string Score { get; set; }
        public string FixtureSpecifier { get; set; }
        public string SelectionSpecifier { get; set; }
        public string CorrelationId { get; set; }
        public double Probability { get; set; }
        public double WonProbability { get; set; }
        public double LostProbability { get; set; }
        public double VoidProbability { get; set; }
        public double HalfWonProbability { get; set; }
        public double HalfLostProbability { get; set; }
        public int SelectionCount { get; set; }
        public BetSelectionState BetSelectionState { get; set; }
    }

    public class BetFixtureTranslationResponseDTO
    {
        public BetFixtureTranslationResponseDTO()
        {
            Selections = new List<BetFixtureSelectionTranslationResponseDTO>();
        }
        public List<BetFixtureSelectionTranslationResponseDTO> Selections { get; set; }
    }

    public class BetFixtureSelectionTranslationResponseDTO
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public long FixtureId { get; set; }
        public string FixtureName { get; set; }
        public long MarketId { get; set; }
        public string MarketName { get; set; }
        public long SelectionId { get; set; }
        public string SelectionName { get; set; }
    }
}
