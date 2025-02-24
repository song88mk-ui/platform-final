using Platform.Shared.Enums;

namespace Platform.RedCat.API.Models
{
    public class RedCatBaseResponseDTO<T>
    {
        public RedCatStatusCode StatusCode { get; set; }
        public string Message { get; set; }
        public T Result { get; set; }
    }
}
