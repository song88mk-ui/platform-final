using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class UserFavoriteResponseDTO
    {
        public UserFavoriteResponseDTO()
        {
            Favorites = new List<UserFavoriteDTO>();
        }
        public List<UserFavoriteDTO> Favorites { get; set; }
    }

    public class UserFavoriteDTO
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int HierarchyId { get; set; }
        public string HierarchyName { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class UserFavoriteRequestDTO
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public string UserId { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }

    public class UserFavoriteInsertDeleteRequestDTO
    {
        public string ClientCode { get; set; }
        public string UserId { get; set; }
        public string Id { get; set; }
        public UserFavoriteHierarchy UserFavoriteHierarchy { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }
}
