using Bellows.Abstractions;

namespace AspNetCoreSample.PipelineBehavior.Features.UserManagement;

public class SlowReportHandler : IRequestHandler<SlowReportRequest, ReportDto>
{
    private readonly ILogger<SlowReportHandler> _logger;

    public SlowReportHandler(ILogger<SlowReportHandler> logger)
    {
        _logger = logger;
    }

    public async Task<ReportDto> Handle(SlowReportRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating report (will take {Duration}ms)...", request.DurationMs);

        // Simulate slow operation
        await Task.Delay(request.DurationMs, cancellationToken);

        return new ReportDto(
            "User Activity Report",
            Random.Shared.Next(100, 1000),
            DateTime.UtcNow
        );
    }
}
