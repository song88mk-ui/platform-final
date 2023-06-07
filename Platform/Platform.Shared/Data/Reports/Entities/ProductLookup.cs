using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Reports.Entities
{
    public class ProductLookup
    {
        public int ProductTypeId { get; set; }

        public string ProductTypeName { get; set; }

        public string GameType { get; set; }

        public string ProviderName { get; set; }

        public string ProductName { get; set; }
    }
}
