namespace BackupManager.Configurations;

public class AppConfig
{
    public const string SectionName = "AppConfig";

    public string HourExecution { get; set; } = "00:00";
    public int RetentionDays { get; set; } = 1;
}
