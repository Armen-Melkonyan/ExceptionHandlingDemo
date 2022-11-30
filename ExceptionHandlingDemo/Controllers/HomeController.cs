using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace ExceptionHandlingDemo.Controllers
{
    [ApiController]
    [Route("test")]
    public class HomeController : ControllerBase
    {
        [Route("get")]
        [HttpGet]
        public string Get()
        {
            return "Hello from Index";
        }

        public string Get2()
        {
            return "Hello from Index";
        }
    }
}
