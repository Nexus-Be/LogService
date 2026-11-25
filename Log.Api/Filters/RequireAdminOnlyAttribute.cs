namespace Log.Api.Filters;

using System;
using Log.Api.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

/// <summary>Only the admin role may call the action; staff are refused even though other routes in the controller allow them.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireAdminOnlyAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        var userId = http.GetUserId();
        if (userId == null || userId == Guid.Empty)
        {
            context.Result = new ObjectResult(new { status = 401, errorCode = "UNAUTHORIZED", detail = "Authentication is required to access this resource." }) { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }

        if (!http.IsAdmin())
        {
            context.Result = new ObjectResult(new { status = 403, errorCode = "FORBIDDEN", detail = "Only administrators can perform this action." }) { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
