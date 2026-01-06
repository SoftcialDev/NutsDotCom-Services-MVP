using Microsoft.Extensions.Logging;

namespace ComplaintAnalysis.Infrastructure.Logging;

public class LoggerAdapter : ILoggerAdapter
{
    private readonly ILogger<LoggerAdapter> _logger;

    public LoggerAdapter(ILogger<LoggerAdapter> logger)
    {
        _logger = logger;
    }

    public void LogInformation(string message, params object[] args)
    {
        _logger.LogInformation(message, args);
    }

    public void LogError(Exception exception, string message, params object[] args)
    {
        _logger.LogError(exception, message, args);
    }

    public void LogWarning(string message, params object[] args)
    {
        _logger.LogWarning(message, args);
    }
}

