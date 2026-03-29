using CleanArchitecture.Core.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Middlewares
{
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception error)
            {
                var response = context.Response;
                response.ContentType = "application/json";
                ProblemDetails problemDetails;

                switch (error)
                {
                    case Core.Exceptions.ApiException e:
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        problemDetails = CreateProblemDetails(response.StatusCode, "Bad Request", e.Message, context);
                        break;
                    case ValidationException e:
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        problemDetails = CreateProblemDetails(response.StatusCode, "Validation Error", "Some validation errors occurred.", context);
                        problemDetails.Extensions["errors"] = e.Errors;
                        break;
                    case KeyNotFoundException e:
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        problemDetails = CreateProblemDetails(response.StatusCode, "Not Found", e.Message, context);
                        break;
                    default:
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        problemDetails = CreateProblemDetails(response.StatusCode, "Internal Server Error", "An unexpected error occurred.", context);
                        break;
                }

                var result = JsonSerializer.Serialize(problemDetails);

                await response.WriteAsync(result);
            }
        }

        private static ProblemDetails CreateProblemDetails(int statusCode, string title, string detail, HttpContext context)
        {
            return new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path,
                Type = $"https://httpstatuses.com/{statusCode}"
            };
        }
    }
}
