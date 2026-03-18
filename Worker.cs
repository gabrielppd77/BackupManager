using BackupManager.Configurations;
using BackupManager.Services;
using Microsoft.Extensions.Options;

namespace BackupManager;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly AppConfig _appConfig;
    private readonly RunBackupService _runBackupService;

    public Worker(
        ILogger<Worker> logger, 
        IOptions<AppConfig> appConfigOptions, 
        RunBackupService runBackupService)
    {
        _logger = logger;
        _appConfig = appConfigOptions.Value;
        _runBackupService = runBackupService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"Worker start at: {DateTimeOffset.Now}");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation($"Worker running at: {DateTimeOffset.Now}");

            var hourExecution = TimeSpan.Parse(_appConfig.HourExecution);

            var nowDate = DateTime.Now;

            var nextExecution = nowDate.Date.Add(hourExecution);

            _logger.LogInformation($"Next execution in: {nextExecution}");

            if (nowDate > nextExecution)
            {
                nextExecution = nextExecution.AddDays(1);
                _logger.LogInformation($"Adjust time to next execution in: {nextExecution}");
            }

            var delay = nextExecution - nowDate;

            await Task.Delay(delay, stoppingToken);

            await _runBackupService.RunAsync();
        }
    }
}
