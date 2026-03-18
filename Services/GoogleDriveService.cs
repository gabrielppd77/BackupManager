using BackupManager.Configurations;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Microsoft.Extensions.Options;

public class GoogleDriveService
{
    private readonly ILogger<GoogleDriveService> _logger;
    private readonly DriveService _driveService;

    private readonly GoogleDrive _googleDrive;
    private readonly AppConfig _appConfig;

    public GoogleDriveService(
        ILogger<GoogleDriveService> logger,
        IOptions<GoogleDrive> googleDriveOptions,
        IOptions<AppConfig> appConfigOptions)
    {
        _logger = logger;
        _googleDrive = googleDriveOptions.Value;
        _appConfig = appConfigOptions.Value;

        var credential = CredentialFactory
            .FromJson(_googleDrive.ServiceAccountJson, JsonCredentialParameters.ServiceAccountCredentialType)
            .CreateScoped(DriveService.ScopeConstants.DriveFile);

        _driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Backup Service"
        });
    }

    public async Task UploadFile(string filePath)
    {
        _logger.LogInformation($"Start upload file - {DateTimeOffset.Now}");

        var fileMetadata = new Google.Apis.Drive.v3.Data.File()
        {
            Name = Path.GetFileName(filePath),
            Parents = new List<string> { _googleDrive.FolderId }
        };

        using var stream = new FileStream(filePath, FileMode.Open);

        var request = _driveService.Files.Create(
            fileMetadata,
            stream,
            "application/zip"
        );

        request.Fields = "id";

        var result = await request.UploadAsync();

        if (result.Status != UploadStatus.Completed)
            throw new Exception(result.Exception.Message);

        _logger.LogInformation($"Finish upload file - {DateTimeOffset.Now}");
    }

    public async Task RemoveOldFiles()
    {
        _logger.LogInformation($"Start remove old files - {DateTimeOffset.Now}");

        var cutoffDate = DateTime.UtcNow.AddDays(-_appConfig.RetentionDays);

        var request = _driveService.Files.List();
        request.Q = $"'{_googleDrive.FolderId}' in parents and trashed = false";
        request.Fields = "files(id, name, createdTime)";
        request.PageSize = 1000;

        var result = await request.ExecuteAsync();

        var files = result.Files?
            .Where(f => f.CreatedTimeDateTimeOffset != null)
            .OrderByDescending(f => f.CreatedTimeDateTimeOffset)
            .ToList();

        if (files == null || files.Count == 0)
        {
            _logger.LogInformation($"Not have files to remove - {DateTimeOffset.Now}");
            return;
        }

        if (files.Count == 1)
        {
            _logger.LogInformation($"Only one backup found. Skipping deletion - {DateTimeOffset.Now}");
            return;
        }

        var oldFiles = files
            .Where(f => f.CreatedTimeDateTimeOffset.Value.UtcDateTime < cutoffDate)
            .ToList();

        if (!oldFiles.Any())
        {
            _logger.LogInformation($"Not have old files to remove - {DateTimeOffset.Now}");
            return;
        }

        var filesToKeepCount = files.Count - oldFiles.Count;

        if (filesToKeepCount <= 0)
        {
            var newest = files.First();

            _logger.LogWarning($"All backups are older than retention. Keeping newest: {newest.Name} - {DateTimeOffset.Now}");

            oldFiles = oldFiles.Where(f => f.Id != newest.Id).ToList();
        }

        foreach (var file in oldFiles)
        {
            _logger.LogInformation($"Deleting old backup: {file.Name} - {DateTimeOffset.Now}");

            await _driveService.Files.Delete(file.Id).ExecuteAsync();
        }

        _logger.LogInformation($"Finish remove old files - {DateTimeOffset.Now}");
    }
}