namespace BackupManager.Configurations;

public class GoogleDrive
{
    public const string SectionName = "GoogleDrive";

    public string ServiceAccountFilePath { get; set; } = "";
    public string FolderId { get; set; } = "";
}
