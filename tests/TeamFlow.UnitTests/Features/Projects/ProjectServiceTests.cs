using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Projects.DTOs;
using TeamFlow.Application.Features.Projects.Services;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;
using Xunit;

namespace TeamFlow.UnitTests.Features.Projects;

public class ProjectServiceTests
{
    private class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public Guid? OrganizationId { get; set; } = Guid.NewGuid();
        public string? Email => "test@teamflow.local";
        public string? Role => "Owner";
        public bool IsAuthenticated => true;
    }

    private static (ProjectService service, ApplicationDbContext context, FakeCurrentUserService currentUserService) CreateService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TeamFlow_Projects_{Guid.NewGuid()}")
            .Options;

        var context = new ApplicationDbContext(options);
        var currentUserService = new FakeCurrentUserService();
        var service = new ProjectService(context, currentUserService);

        return (service, context, currentUserService);
    }

    [Fact]
    public async Task CreateProjectAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var (service, context, currentUserService) = CreateService();
        var org = new Organization { Id = currentUserService.OrganizationId!.Value, Name = "Acme Corp", Slug = "acme" };
        context.Organizations.Add(org);
        await context.SaveChangesAsync();

        var request = new CreateProjectRequest("Core API", "CORE", "Core platform redesign", null);

        // Act
        var result = await service.CreateProjectAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("CORE", result.Value.Key);
        Assert.Equal("Core API", result.Value.Name);

        var savedProject = await context.Projects.FirstOrDefaultAsync(p => p.Key == "CORE");
        Assert.NotNull(savedProject);
        Assert.Equal(currentUserService.OrganizationId, savedProject.OrganizationId);
    }

    [Fact]
    public async Task CreateProjectAsync_WithDuplicateKeyInSameOrg_ReturnsConflict()
    {
        // Arrange
        var (service, context, currentUserService) = CreateService();
        var org = new Organization { Id = currentUserService.OrganizationId!.Value, Name = "Acme Corp", Slug = "acme" };
        context.Organizations.Add(org);
        await context.SaveChangesAsync();

        var request1 = new CreateProjectRequest("Project A", "ENG", "First", null);
        var request2 = new CreateProjectRequest("Project B", "ENG", "Duplicate Key", null);

        await service.CreateProjectAsync(request1);

        // Act
        var result = await service.CreateProjectAsync(request2);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Project.KeyExists", result.Error.Code);
    }
}

