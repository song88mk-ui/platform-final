using Microsoft.AspNetCore.Mvc;
using Platform.API.Models;

namespace Platform.API.Controllers
{
    //
    public class BaseController : ControllerBase
    {
        public OGClientRequest OGParameters { get; set; }

    }
}