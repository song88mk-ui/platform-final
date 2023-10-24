using Platform.Shared.Enums;
using System.Security.Policy;

namespace Platform.API.Models
{
    public class InsertDeleteUserFavoritesRequest
    {
        public int SportId { get; set; }
        public string Id { get; set; }
        public UserFavoriteHierarchy UserFavoriteHierarchy { get; set; }
    }
}
