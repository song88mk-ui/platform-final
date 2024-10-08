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
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
