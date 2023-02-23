using Platform.Operator.API.Enums;

namespace Platform.Operator.API.Models
{
    public class BaseResponse<T>
    {
        public AuthStatusCode StatusCode { get; set; }
        public string Message { get; set; }
        public T Result { get; set; }
    }
}
