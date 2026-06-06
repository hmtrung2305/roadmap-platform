using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using RoadmapPlatform.Application.Interfaces.Security;
using System.Reflection;

namespace RoadmapPlatform.Api.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RequireCaptchaAttribute : Attribute, IAsyncActionFilter
{
    private readonly string? _action;

    public RequireCaptchaAttribute(string? action = null)
    {
        _action = action;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var captchaService = context.HttpContext.RequestServices
            .GetRequiredService<ICaptchaService>();

        var token = FindCaptchaToken(context.ActionArguments);
        var remoteIp = context.HttpContext.Connection.RemoteIpAddress?.ToString();

        var verification = await captchaService.VerifyAsync(
            token,
            _action,
            remoteIp,
            context.HttpContext.RequestAborted);

        if (!verification.Success)
        {
            context.Result = new ObjectResult(new
            {
                message = verification.Message ?? "CAPTCHA verification failed.",
                errors = verification.ErrorCodes
            })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };

            return;
        }

        await next();
    }

    private static string? FindCaptchaToken(IDictionary<string, object?> actionArguments)
    {
        foreach (var argument in actionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            if (argument is ICaptchaProtectedRequest captchaRequest)
            {
                return captchaRequest.CaptchaToken;
            }

            var captchaTokenProperty = argument
                .GetType()
                .GetProperty(
                    nameof(ICaptchaProtectedRequest.CaptchaToken),
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.IgnoreCase);

            if (captchaTokenProperty?.PropertyType == typeof(string))
            {
                return captchaTokenProperty.GetValue(argument) as string;
            }
        }

        return null;
    }
}