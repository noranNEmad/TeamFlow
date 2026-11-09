namespace TeamFlow.Domain.Enums;

public enum OrganizationRole
{
    Member = 0,
    Admin = 1,
    Owner = 2
}

public enum TeamRole
{
    Member = 0,
    Lead = 1
}

public enum ProjectStatus
{
    Planning = 0,
    Active = 1,
    OnHold = 2,
    Completed = 3,
    Archived = 4
}

public enum TaskItemStatus
{
    Backlog = 0,
    Todo = 1,
    InProgress = 2,
    InReview = 3,
    Done = 4
}

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3
}
