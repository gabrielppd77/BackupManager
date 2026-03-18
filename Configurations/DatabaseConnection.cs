namespace BackupManager.Configurations;

public class DatabaseConnection
{
    public const string SectionName = "DatabaseConnection";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string Username { get; set; } = "postgres";
    public string Password { get; set; } = "123456";
}