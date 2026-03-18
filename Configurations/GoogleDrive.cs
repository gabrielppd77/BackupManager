namespace BackupManager.Configurations;

public class GoogleDrive
{
    public const string SectionName = "GoogleDrive";

    public string ServiceAccountJson { get; set; } = "";
    public string FolderId { get; set; } = "";
}
