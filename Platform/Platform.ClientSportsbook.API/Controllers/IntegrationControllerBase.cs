using Microsoft.AspNetCore.Mvc;
using Platform.ClientSportsbook.API.Models;
using Platform.Shared.Enums;
using System;

namespace Platform.ClientSportsbook.API.Controllers
{
    public class IntegrationControllerBase : ControllerBase
    {
        public ClientKeyFilter ClientKeyFilter { get; set; }
    }
}
