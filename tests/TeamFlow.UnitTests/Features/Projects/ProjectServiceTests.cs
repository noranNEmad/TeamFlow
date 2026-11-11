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

    private class FakeCacheService : ICacheService
    {
        public readonly Dictionary<string, object> Storage = new();
        public int GetCallCount;
        public int SetCallCount;
        public int RemoveCallCount;

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            GetCallCount++;
            if (Storage.TryGetValue(key, out var val))
            {
                return Task.FromResult((T?)val);
            }
            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            SetCallCount++;
            Storage[key] = value!;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            RemoveCallCount++;
            Storage.Remove(key);
            return Task.CompletedTask;
        }
    }

    private static (ProjectService service, ApplicationDbContext context, FakeCurrentUserService currentUserService, FakeCacheService cacheService) CreateService(bool withCache = false)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TeamFlow_Projects_{Guid.NewGuid()}")
            .Options;

        var currentUserService = new FakeCurrentUserService();
        var cacheService = new FakeCacheService();
        var context = new ApplicationDbContext(options, currentUserService);
        var service = new ProjectService(context, currentUserService, withCache ? cacheService : null);

        return (service, context, currentUserService, cacheService);
    }

    [Fact]
    public async Task CreateProjectAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var (service, context, currentUserService, _) = CreateService();
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
        var (service, context, currentUserService, _) = CreateService();
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

    [Fact]
    public async Task GetProjectsAsync_UsesCache_And_CreateProject_InvalidatesCache()
    {
        // Arrange
        var (service, context, currentUserService, cacheService) = CreateService(withCache: true);
        var org = new Organization { Id = currentUserService.OrganizationId!.Value, Name = "Acme Corp", Slug = "acme" };
        context.Organizations.Add(org);
        await context.SaveChangesAsync();

        // 1. Initial Get: Cache Miss -> Queries DB & Sets Cache
        var list1 = await service.GetProjectsAsync();
        Assert.True(list1.IsSuccess);
        Assert.Equal(1, cacheService.GetCallCount);
        Assert.Equal(1, cacheService.SetCallCount);

        // 2. Second Get: Cache Hit -> Returns Cached directly
        var list2 = await service.GetProjectsAsync();
        Assert.True(list2.IsSuccess);
        Assert.Equal(2, cacheService.GetCallCount);
        Assert.Equal(1, cacheService.SetCallCount); // Did not set again

        // 3. Create Project -> Invalidates (Removes) Tenant Cache
        var createResult = await service.CreateProjectAsync(new CreateProjectRequest("New Proj", "NEW", "Desc", null));
        Assert.True(createResult.IsSuccess);
        Assert.Equal(1, cacheService.RemoveCallCount);

        // 4. Third Get: Cache Miss again -> Refreshes Cache with new project
        var list3 = await service.GetProjectsAsync();
        Assert.True(list3.IsSuccess);
        Assert.Single(list3.Value);
        Assert.Equal(3, cacheService.GetCallCount);
        Assert.Equal(2, cacheService.SetCallCount);
    }
}

