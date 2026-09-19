using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Domain.Common;

namespace VolleyHub.Api.ExceptionHandling
{
    public sealed class GlobalExceptionHandler : IExceptionHandler
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
            var problemDetails = CreateProblemDetails(httpContext, exception);

            if (problemDetails.Status >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception occurred.");
            }
            else
            {
                _logger.LogWarning(exception, "Handled exception occurred.");
            }

            httpContext.Response.StatusCode = problemDetails.Status
                ?? StatusCodes.Status500InternalServerError;

            await httpContext.Response.WriteAsJsonAsync(
                problemDetails,
                options: null,
                contentType: "application/problem+json",
                cancellationToken: cancellationToken);

            return true;
        }

        private static ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            Exception exception)
        {
            return exception switch
            {
                ValidationException validationException =>
                    CreateValidationProblemDetails(httpContext, validationException),

                UnauthorizedException unauthorizedException =>
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status401Unauthorized,
                        Title = "Unauthorized",
                        Detail = unauthorizedException.Message,
                        Instance = httpContext.Request.Path
                    },

                ForbiddenAccessException forbiddenAccessException =>
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status403Forbidden,
                        Title = "Forbidden",
                        Detail = forbiddenAccessException.Message,
                        Instance = httpContext.Request.Path
                    },

                NotFoundException notFoundException =>
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "Not found",
                        Detail = notFoundException.Message,
                        Instance = httpContext.Request.Path
                    },

                ConflictException conflictException =>
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status409Conflict,
                        Title = "Conflict",
                        Detail = conflictException.Message,
                        Instance = httpContext.Request.Path
                    },

                BusinessRuleException businessRuleException =>
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status409Conflict,
                        Title = "Conflict",
                        Detail = businessRuleException.Message,
                        Instance = httpContext.Request.Path
                    },

                ArgumentException argumentException =>
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Bad request",
                        Detail = argumentException.Message,
                        Instance = httpContext.Request.Path
                    },

                _ =>
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status500InternalServerError,
                        Title = "Server error",
                        Detail = "An unexpected error occurred.",
                        Instance = httpContext.Request.Path
                    }
            };
        }

        private static ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(error => error.ErrorMessage)
                        .ToArray());

            return new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation error",
                Detail = "One or more validation errors occurred.",
                Instance = httpContext.Request.Path
            };
        }
    }
}
