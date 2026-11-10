using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Features.Auth.Services;
using TeamFlow.Application.Features.Projects.Services;
using TeamFlow.Application.Features.Tasks.Services;
using TeamFlow.Application.Features.Teams.Services;

namespace TeamFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ITeamService, TeamService>();

        return services;
    }
}
