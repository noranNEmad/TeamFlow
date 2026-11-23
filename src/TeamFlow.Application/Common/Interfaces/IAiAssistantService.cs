namespace TeamFlow.Application.Common.Interfaces;

public record TaskAiBreakdownResponse(
    string FormattedMarkdown,
    int SuggestedPriority, // 0 = Low, 1 = Medium, 2 = High, 3 = Urgent
    IReadOnlyList<string> AcceptanceCriteria,
    string Summary
);

public interface IAiAssistantService
{
    Task<TaskAiBreakdownResponse> GenerateTaskBreakdownAsync(string title, string? context = null, CancellationToken cancellationToken = default);
    Task<string> SummarizeCommentsAsync(IReadOnlyList<string> comments, CancellationToken cancellationToken = default);
}
