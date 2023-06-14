using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.Models
{
    public class UserBetConfigDetailsRequestDTO
    {
        public string ClientId { get; set; } = "";
        public string UserId { get; set; }
    }
    public class UserBetConfigRequestDTO
    {
        public string UserId { get; set; }
        public string ClientId { get; set; }
        public string CurrencyCode { get; set; }
        public string VerticalConfig { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    public class UserBetConfigResponseDTO
    {
        public UserBetConfigResponseDTO()
        {
            VerticalConfig = new VerticalConfig(true);
        }
        public string UserId { get; set; }
        public string ClientId { get; set; }
        public string CurrencyCode { get; set; }
        public VerticalConfig VerticalConfig { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    public class VerticalConfig
    {
        public VerticalConfig(bool value)
        {
            IsSoccerEnabled = value;
            IsSportsEnabled = value;
            IsParlayEnabled = value;
            IsCasinoEnabled = value;
            IsLiveCasinoEnabled = value;
            IsVirtualGamesEnabled = value;
            IsCricketEnabled = value;
        }
        public bool IsSoccerEnabled { get; set; }
        public bool IsSportsEnabled { get; set; }
        public bool IsParlayEnabled { get; set; }
        public bool IsCasinoEnabled { get; set; }
        public bool IsLiveCasinoEnabled { get; set; }
        public bool IsVirtualGamesEnabled { get; set; }
        public bool IsCricketEnabled { get; set; }
    }
}
