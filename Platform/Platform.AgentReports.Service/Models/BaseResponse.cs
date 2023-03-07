namespace Platform.AgentReports.Service.Models
{
    public class BaseResponse<T>
    {
        public int StatusCode { get; set; }
        public string StatusName { get; set; }
        public T Result { get; set; }
    }
}
