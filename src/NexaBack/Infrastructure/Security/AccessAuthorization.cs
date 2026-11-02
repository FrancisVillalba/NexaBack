using Microsoft.AspNetCore.Authorization;

namespace NexaBack.Infrastructure.Security;

public sealed class RequireAccessAttribute(string codigo) : AuthorizeAttribute("acceso:" + codigo);
public sealed record AccessRequirement(string Codigo) : IAuthorizationRequirement;
public sealed class AccessHandler : AuthorizationHandler<AccessRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AccessRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            context.User.HasClaim("acceso", requirement.Codigo))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
