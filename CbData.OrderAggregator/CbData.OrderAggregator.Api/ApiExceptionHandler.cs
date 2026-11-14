using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CbData.OrderAggregator.Api;

//v tuhle chvíli ExceptionHandler řeší pouze problém, kdy se nečitelný json v http požadavku vracel jako 500 (selhala deserializace), ačkoli to je 400
//custom výjimky v tuhle chvíli nemám, nicméně jejich zpracování b bylo zde
internal sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            BadHttpRequestException badRequest => new ProblemDetails
            {
                Status = badRequest.StatusCode,
                Title = "Invalid request.",
                Detail = badRequest.Message
            },

            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred."
            }
        };

        if (problemDetails.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.UnhandledException(exception);
        }
        else
        {
            logger.BadRequest(exception);
        }

        httpContext.Response.StatusCode = problemDetails.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }
}