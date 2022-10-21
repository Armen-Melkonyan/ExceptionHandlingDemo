using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExceptionHandlingDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ErrorController : ControllerBase
    {

        [Route("/error-local-development")]
        public IActionResult ErrorLocalDevelopment()
        {
            return Ok("This is an error");
        } // Add extra details here
        //public IActionResult ErrorLocalDevelopment() => Problem(); // Add extra details here

        [Route("/error")]
        public IActionResult Error() => Problem();
    }
}
