using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Features.Auth.Services;

namespace TeamFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
