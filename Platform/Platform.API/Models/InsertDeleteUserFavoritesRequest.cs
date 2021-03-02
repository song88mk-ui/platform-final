using Platform.Shared.Enums;

namespace Platform.API.Models
{
    public class InsertDeleteUserFavoritesRequest
    {
        public string Id { get; set; }
        public UserFavoriteHierarchy UserFavoriteHierarchy { get; set; }
    }
}
