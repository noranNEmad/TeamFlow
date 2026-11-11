using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Tasks.DTOs;
using TeamFlow.Application.Features.Tasks.Services;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;
using Xunit;

namespace TeamFlow.UnitTests.Features.Tasks;

public class TaskServiceTests
{
    private class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public Guid? OrganizationId { get; set; } = Guid.NewGuid();
        public string? Email => "test@teamflow.local";
        public string? Role => "Owner";
        public bool IsAuthenticated => true;
    }

    private static (TaskService service, ApplicationDbContext context, FakeCurrentUserService currentUserService, Project project) CreateServiceWithProject()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TeamFlow_Tasks_{Guid.NewGuid()}")
            .Options;

        var currentUserService = new FakeCurrentUserService();
        var context = new ApplicationDbContext(options, currentUserService);

        var user = new User
        {
            Id = currentUserService.UserId!.Value,
            FullName = "Lead Engineer",
            Email = "lead@teamflow.local",
            PasswordHash = "hash"
        };
        var org = new Organization
        {
            Id = currentUserService.OrganizationId!.Value,
            Name = "Acme Corp",
            Slug = "acme"
        };
        var project = new Project
        {
            OrganizationId = org.Id,
            Name = "Platform",
            Key = "PLT",
            Status = ProjectStatus.Active
        };

        context.Users.Add(user);
        context.Organizations.Add(org);
        context.Projects.Add(project);
        context.SaveChanges();

        var service = new TaskService(context, currentUserService);

        return (service, context, currentUserService, project);
    }

    [Fact]
    public async Task CreateTaskAsync_WithValidInput_ReturnsSuccess()
    {
        // Arrange
        var (service, context, currentUserService, project) = CreateServiceWithProject();
        var request = new CreateTaskRequest(
            ProjectId: project.Id,
            Title: "Setup CI/CD pipeline",
            Description: "Configure GitHub Actions",
            Status: TaskItemStatus.Todo,
            Priority: TaskPriority.High,
            AssigneeId: null,
            DueDate: DateTimeOffset.UtcNow.AddDays(7)
        );

        // Act
        var result = await service.CreateTaskAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Setup CI/CD pipeline", result.Value.Title);
        Assert.Equal(TaskItemStatus.Todo, result.Value.Status);
        Assert.Equal(TaskPriority.High, result.Value.Priority);
        Assert.Equal("Lead Engineer", result.Value.CreatorName);

        var savedTask = await context.Tasks.FirstOrDefaultAsync(t => t.Id == result.Value.Id);
        Assert.NotNull(savedTask);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_UpdatesStatus_Successfully()
    {
        // Arrange
        var (service, _, _, project) = CreateServiceWithProject();
        var createRequest = new CreateTaskRequest(
            project.Id,
            "Implement Auth",
            "JWT integration",
            TaskItemStatus.Todo,
            TaskPriority.Urgent,
            null,
            null
        );
        var created = await service.CreateTaskAsync(createRequest);

        // Act (Drag-and-drop on Kanban board: Todo -> InProgress)
        var updateResult = await service.UpdateTaskStatusAsync(created.Value.Id, TaskItemStatus.InProgress);

        // Assert
        Assert.True(updateResult.IsSuccess);
        Assert.Equal(TaskItemStatus.InProgress, updateResult.Value.Status);
    }

    [Fact]
    public async Task AddCommentAsync_AttachesCommentToTask_Successfully()
    {
        // Arrange
        var (service, context, currentUserService, project) = CreateServiceWithProject();
        var createRequest = new CreateTaskRequest(project.Id, "Design Review", null, TaskItemStatus.InProgress, TaskPriority.Medium, null, null);
        var created = await service.CreateTaskAsync(createRequest);

        // Act
        var commentResult = await service.AddCommentAsync(created.Value.Id, new CreateCommentRequest("PR is ready for review!"));

        // Assert
        Assert.True(commentResult.IsSuccess);
        Assert.Equal("PR is ready for review!", commentResult.Value.Content);
        Assert.Equal("Lead Engineer", commentResult.Value.AuthorName);

        var comments = await service.GetCommentsAsync(created.Value.Id);
        Assert.True(comments.IsSuccess);
        Assert.Single(comments.Value);
    }
}

