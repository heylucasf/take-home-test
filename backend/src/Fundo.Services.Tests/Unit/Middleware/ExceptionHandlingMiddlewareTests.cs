using FluentAssertions;
using Fundo.Applications.WebApi.Middleware;
using Microsoft.AspNetCore.Http;
using Moq;
using Serilog;
using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Fundo.Services.Tests.Unit.Middleware
{
    public class ExceptionHandlingMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_WhenNoExceptionOccurs_ShouldCallNextDelegate()
        {
            var nextCalled = false;
            RequestDelegate next = (HttpContext context) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new ExceptionHandlingMiddleware(next);
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(context);

            nextCalled.Should().BeTrue();
            context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Fact]
        public async Task InvokeAsync_WhenExceptionOccurs_ShouldReturn500WithErrorResponse()
        {
            var exceptionMessage = "Test exception";
            RequestDelegate next = (HttpContext context) =>
            {
                throw new Exception(exceptionMessage);
            };

            var middleware = new ExceptionHandlingMiddleware(next);
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(context);

            context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
            context.Response.ContentType.Should().Be("application/json");

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();

            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            errorResponse.Should().NotBeNull();
            errorResponse!.Message.Should().Be("An error occurred while processing your request.");
            errorResponse.Detail.Should().Be(exceptionMessage);
            errorResponse.ErrorId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task InvokeAsync_WhenInvalidOperationExceptionOccurs_ShouldReturn500()
        {
            RequestDelegate next = (HttpContext context) =>
            {
                throw new InvalidOperationException("Invalid operation");
            };

            var middleware = new ExceptionHandlingMiddleware(next);
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(context);

            context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task InvokeAsync_WhenArgumentExceptionOccurs_ShouldReturn500()
        {
            RequestDelegate next = (HttpContext context) =>
            {
                throw new ArgumentException("Invalid argument");
            };

            var middleware = new ExceptionHandlingMiddleware(next);
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(context);

            context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        }

        private class ErrorResponse
        {
            public string ErrorId { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string Detail { get; set; } = string.Empty;
        }
    }
}