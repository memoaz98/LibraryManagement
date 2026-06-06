using FluentValidation;
using LibraryManagement.Application.Common.Exceptions;
using LibraryManagement.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.WebApi.ExceptionHandlers;

/// <summary>
/// Translates exceptions thrown by lower layers into RFC 7807 ProblemDetails
/// HTTP responses. Registered as a global IExceptionHandler in Program.cs.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            ValidationException validationEx => HandleValidationException(validationEx),
            NotFoundException notFoundEx => HandleNotFoundException(notFoundEx),
            ConflictException conflictEx => HandleConflictException(conflictEx),
            DomainException domainEx => HandleDomainException(domainEx),
            _ => HandleUnexpectedException(exception)
        };

        _logger.LogWarning(
            exception,
            "Request {Method} {Path} failed with {ExceptionType}: {Message}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            exception.GetType().Name,
            exception.Message);

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static ProblemDetails HandleValidationException(ValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        };
    }

    private static ProblemDetails HandleNotFoundException(NotFoundException ex)
    {
        return new ProblemDetails
        {
            Title = "Resource not found.",
            Status = StatusCodes.Status404NotFound,
            Detail = ex.Message,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
        };
    }

    private static ProblemDetails HandleConflictException(ConflictException ex)
    {
        return new ProblemDetails
        {
            Title = "Conflict with current state.",
            Status = StatusCodes.Status409Conflict,
            Detail = ex.Message,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"
        };
    }

    private static ProblemDetails HandleDomainException(DomainException ex)
    {
        return new ProblemDetails
        {
            Title = "Business rule violation.",
            Status = StatusCodes.Status400BadRequest,
            Detail = ex.Message,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        };
    }

    private static ProblemDetails HandleUnexpectedException(Exception ex)
    {
        return new ProblemDetails
        {
            Title = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "Please contact support if the problem persists.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };
    }
}