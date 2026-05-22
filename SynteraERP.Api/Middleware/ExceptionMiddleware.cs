using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.DTOs.Common;

namespace SynteraERP.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message) = ex switch
        {
            KeyNotFoundException => (HttpStatusCode.NotFound, ex.Message),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Akses ditolak."),
            InvalidOperationException => (HttpStatusCode.BadRequest, ex.Message),
            ArgumentException => (HttpStatusCode.BadRequest, ex.Message),
            DbUpdateException dbEx => (HttpStatusCode.InternalServerError,
                dbEx.InnerException?.Message is { } inner && inner.Contains("UNIQUE")
                    ? "Gagal menyimpan: data duplikat terdeteksi. Silakan coba lagi."
                    : "Gagal menyimpan data ke database. Silakan coba lagi."),
            _ => (HttpStatusCode.InternalServerError, "Terjadi kesalahan server. Silakan coba lagi.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse.Fail(message);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(json);
    }
}
