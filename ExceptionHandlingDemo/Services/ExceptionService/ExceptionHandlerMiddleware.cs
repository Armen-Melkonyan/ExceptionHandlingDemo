using ExceptionHandlingDemo.Models;
//using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources;
using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

//using System;
//using System.Diagnostics;
//using System.Runtime.ExceptionServices;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Http.Features;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using Microsoft.Net.Http.Headers;

namespace ExceptionHandlingDemo.Services.ExceptionService
{
    public class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ExceptionHandlerOptions _options;
        private readonly ILogger _logger;
        private readonly Func<object, Task> _clearCacheHeadersDelegate;
        private readonly DiagnosticListener _diagnosticListener;

        public ExceptionHandlerMiddleware(
            RequestDelegate next,
            ExceptionHandlerOptions options,
            ILogger logger,
            Func<object, Task> clearCacheHeadersDelegate,
            DiagnosticListener diagnosticListener
            )
        {
            _clearCacheHeadersDelegate = clearCacheHeadersDelegate;
            _options = options;
            _logger = logger;
            _diagnosticListener = diagnosticListener;
            _next = next;
            if (_options.ExceptionHandler == null)
            {
                if (_options.ExceptionHandlingPath == null)
                {
                    //throw new InvalidOperationException(Resources.ExceptionHandlerOptions_NotConfiguredCorrectly);
                }
                else
                {
                    _options.ExceptionHandler = _next;
                }
            }
        }

        public Task Invoke(HttpContext context)
        {
            ExceptionDispatchInfo edi;

            try
            {
                var task = _next(context);

                if (!task.IsCompletedSuccessfully)
                {
                    return Awaited(this, context, task);
                }
                else
                {
                    return Task.CompletedTask;
                }
            }
            catch (Exception exception)
            {

                // Get the Exception, but don't continue processing in the catch block as its bad for stack usage.
                edi = ExceptionDispatchInfo.Capture(exception);
            }

            return HandleException(context, edi);


            static async Task Awaited(ExceptionHandlerMiddleware middleware, HttpContext context, Task task)
            {
                ExceptionDispatchInfo edi = null;
                try
                {
                    await task;
                }
                catch (Exception exception)
                {
                    // Get the Exception, but don't continue processing in the catch block as its bad for stack usage.
                    edi = ExceptionDispatchInfo.Capture(exception);
                }

                if (edi != null)
                {
                    await middleware.HandleException(context, edi);
                }
            }

        }

        private async Task HandleException(HttpContext context, ExceptionDispatchInfo edi)
        {
            //_logger.UnhandledException(edi.SourceException); //   error

            // We can't do anything if the response has already started, just abort.
            if (context.Response.HasStarted)
            {
                //_logger.ResponseStartedErrorHandler(); //   error

                edi.Throw();
            }

            PathString originalPath = context.Request.Path;
            if (_options.ExceptionHandlingPath.HasValue)
            {
                context.Request.Path = _options.ExceptionHandlingPath;
            }
            try
            {
                ClearHttpContext(context);

                var exceptionHandlerFeature = new ExceptionHandlerFeature()
                {
                    Error = edi.SourceException,
                    Path = originalPath.Value,
                };
                context.Features.Set<IExceptionHandlerFeature>(exceptionHandlerFeature);
                context.Features.Set<IExceptionHandlerPathFeature>(exceptionHandlerFeature);
                context.Response.StatusCode = 500;
                context.Response.OnStarting(_clearCacheHeadersDelegate, context.Response);

                await _options.ExceptionHandler(context);

                if (_diagnosticListener.IsEnabled() && _diagnosticListener.IsEnabled("Microsoft.AspNetCore.Diagnostics.HandledException"))
                {
                    _diagnosticListener.Write("Microsoft.AspNetCore.Diagnostics.HandledException", new { httpContext = context, exception = edi.SourceException });
                }

                // TODO: Optional re-throw? We'll re-throw the original exception by default if the error handler throws.
                return;
            }
            catch (Exception ex2)
            {
                // Suppress secondary exceptions, re-throw the original.

                //_logger.ErrorHandlerException(ex2); //    error
            }
            finally
            {
                context.Request.Path = originalPath;
            }

            edi.Throw(); // Re-throw the original if we couldn't handle it
        }

        private static void ClearHttpContext(HttpContext context)
        {
            context.Response.Clear();

            // An endpoint may have already been set. Since we're going to re-invoke the middleware pipeline we need to reset
            // the endpoint and route values to ensure things are re-calculated.
            context.SetEndpoint(endpoint: null);
            var routeValuesFeature = context.Features.Get<IRouteValuesFeature>();
            routeValuesFeature?.RouteValues?.Clear();
        }

        private static Task ClearCacheHeaders(object state)
        {
            var headers = ((HttpResponse)state).Headers;
            headers[HeaderNames.CacheControl] = "no-cache";
            headers[HeaderNames.Pragma] = "no-cache";
            headers[HeaderNames.Expires] = "-1";
            headers.Remove(HeaderNames.ETag);
            return Task.CompletedTask;
        }

    }
}
