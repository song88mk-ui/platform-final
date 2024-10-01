using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Sportsbook.SQLEntities
{
    [Table("announcements")]
    public class Announcement
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string LangCode { get; set; }
        public bool IsActive { get; set; }
    }
}
