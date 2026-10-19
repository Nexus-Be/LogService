using Log.Domain.Base;
using Log.Domain.Enums;

namespace Log.Domain.Entities;

public class AppLogFileCatalog : BaseEntity<Guid>
{
    public required string ServiceName { get; set; }
    public required AppEnvironment Environment { get; set; } = AppEnvironment.Production;
    public required DateTimeOffset LogDate { get; set; }
    public required string FilePath { get; set; }
    public long FileSizeBytes { get; set; }
    public string? Checksum { get; set; }
    public int TotalLines { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDisabled { get; set; }
}
