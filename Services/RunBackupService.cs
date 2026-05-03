using System.Diagnostics;
using System.IO.Compression;
using BackupManager.Configurations;
using Microsoft.Extensions.Options;
using Npgsql;

namespace BackupManager.Services;

public class RunBackupService
{
    private readonly ILogger<RunBackupService> _logger;
    private readonly DatabaseConnection _databaseConnection;
    private readonly GoogleDriveService _googleDriveService;

    private readonly string _mainFolder;
    private readonly string _backupsFolder;

    public RunBackupService(
        ILogger<RunBackupService> logger,
        IOptions<DatabaseConnection> databaseConnectionOptions,
        IOptions<AppConfig> appConfigOptions,
        GoogleDriveService googleDriveService)
    {
        _logger = logger;
        _databaseConnection = databaseConnectionOptions.Value;
        _googleDriveService = googleDriveService;
        _mainFolder = Path.Combine(Path.GetTempPath(), "backup-service");
        _backupsFolder = Path.Combine(_mainFolder, "backups");
    }

    public async Task RunAsync()
    {

        var databases = await ReadDatabases();

        if (Directory.Exists(_mainFolder))
            Directory.Delete(_mainFolder, true);

        Directory.CreateDirectory(_mainFolder);
        Directory.CreateDirectory(_backupsFolder);

        foreach (var database in databases)
        {
            await DumpDatabase(database);
        }

        _logger.LogInformation($"Start compression - {DateTimeOffset.Now}");

        var zipPath = Path.Combine(_mainFolder, $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip");

        ZipFile.CreateFromDirectory(
            _backupsFolder,
            zipPath
        );

        _logger.LogInformation($"Finish compression - {DateTimeOffset.Now}");

        await _googleDriveService.UploadFile(zipPath);

        await _googleDriveService.RemoveOldFiles();

        Directory.Delete(_mainFolder, true);

        _logger.LogInformation($"Finish process - {DateTimeOffset.Now}");
    }

    private async Task<List<string>> ReadDatabases()
    {
        var databases = new List<string>();

        await using var conn = new NpgsqlConnection($"Host={_databaseConnection.Host};Port={_databaseConnection.Port};Username={_databaseConnection.Username};Password={_databaseConnection.Password}");

        await conn.OpenAsync();

        _logger.LogInformation($"Connected on database - {DateTimeOffset.Now}");

        var sql = @"
            SELECT datname
            FROM pg_database
            WHERE datistemplate = false
            AND datname <> 'postgres'
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            databases.Add(reader.GetString(0));

        _logger.LogInformation($"Databases to dump: {string.Join(", ", databases)} - {DateTimeOffset.Now}");

        return databases;
    }

    private async Task DumpDatabase(string database)
    {
        var fileName = $"{database}_{DateTime.Now:yyyyMMdd_HHmmss}.dump";
        var filePath = Path.Combine(_backupsFolder, fileName);

        _logger.LogInformation($"Creating dump: {fileName} - {DateTimeOffset.Now}");

        var process = new Process();

        process.StartInfo.FileName = "pg_dump";
        process.StartInfo.Arguments =
            $"-h {_databaseConnection.Host} -p {_databaseConnection.Port} -U {_databaseConnection.Username} -d {database} -F c -f \"{filePath}\"";

        process.StartInfo.UseShellExecute = false;

        process.StartInfo.Environment["PGPASSWORD"] = _databaseConnection.Password;

        process.Start();

        await process.WaitForExitAsync();

        _logger.LogInformation($"Finish dump: {fileName} - {DateTimeOffset.Now}");
    }
}
