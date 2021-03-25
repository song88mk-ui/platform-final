using System;

namespace Platform.API.Models
{
    public class SportsRequest
    {
        public int ClientId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
