using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using BaezStone.Fullstack.Api.Dtos;

namespace BaezStone.Fullstack.Api.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // Cliente desconectado: no es un error de servidor.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");

                if (context.Response.HasStarted)
                {
                    throw;
                }

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                var response = new ResponseDto
                {
                    IsSuccess = false,
                    DisplayMessage = "Se ha producido un error inesperado.",
                    ErrorMessages = _env.IsDevelopment()
                        ? new List<string> { ex.ToString() }
                        : new List<string>()
                };

                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}
