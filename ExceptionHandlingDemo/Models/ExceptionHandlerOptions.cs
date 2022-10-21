using Microsoft.AspNetCore.Http;

namespace ExceptionHandlingDemo.Models
{
    public class ExceptionHandlerOptions
    {
        public PathString ExceptionHandlingPath { get; set; }
        public RequestDelegate ExceptionHandler { get; set; }
    }
}
