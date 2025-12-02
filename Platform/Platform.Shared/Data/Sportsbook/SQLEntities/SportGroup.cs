using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Sportsbook.SQLEntities
{
    [Table("SportGroup2")]
    public class SportGroup
    {
        public int SportGroupId { get; set; }
        public string SportGroupName { get; set; }
        public string Sports { get; set; }
    }
}
