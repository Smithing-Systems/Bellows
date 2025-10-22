using Bellows.Abstractions;

namespace AspNetCoreSample.PipelineBehavior.Features.UserManagement;

/// <summary>
/// Request that takes a long time (demonstrates performance monitoring)
/// </summary>
public record SlowReportRequest(int DurationMs) : IRequest<ReportDto>;

public record ReportDto(string ReportName, int RecordCount, DateTime GeneratedAt);
