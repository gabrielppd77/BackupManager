using BackupManager;
using BackupManager.Configurations;
using BackupManager.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();

builder.Services.AddSingleton<RunBackupService>();
builder.Services.AddSingleton<GoogleDriveService>();

builder.Services.Configure<AppConfig>(builder.Configuration.GetSection(AppConfig.SectionName));
builder.Services.Configure<DatabaseConnection>(builder.Configuration.GetSection(DatabaseConnection.SectionName));
builder.Services.Configure<GoogleDrive>(builder.Configuration.GetSection(GoogleDrive.SectionName));

var host = builder.Build();
host.Run();
