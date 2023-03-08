using System;

namespace Platform.AgentReports.API.Models.Response
{
    public class UserDetailsResponseDTO
    {
        public long UserId { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string TelephoneNumber { get; set; }
        public int StatusId { get; set; }
        public int UserTypeId { get; set; }
        public bool IsDeleted { get; set; }
        public int ChatApplicationId { get; set; }
        public string ChatName { get; set; }
        public string EmailAddress { get; set; }
        public bool ResetSecurityCode { get; set; }
        public long CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public long ModifiedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string Currency { get; set; }
        public bool IsCashMarket { get; set; }
    }
}
