using Platform.Shared.Data.Platform.Entities;
using System;

namespace Platform.Shared.Models
{
    public class ClientStatusRequestDTO
    {
        public int ClientIdRef { get; set; }
        public bool IsActive { get; set; }
    }

}
